using besti2.Application.Bookings;
using besti2.Application.Common;
using besti2.Application.Email;
using besti2.Application.Invites;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services.Bookings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Invites;

// BESTI sammen. Rules (two people decided 2026-10-23, groups 2026-10-28): nothing is held before someone says
// yes; one shared link for everyone; each "Jeg blir med!" checks the time again and books that person at once
// (the inviter's own visit together with the first yes). The group closes when it is full or the answer time is
// up; then the business gets the final count. A waiting invitation runs out "lazily" when someone reads it,
// and the job every 15 minutes closes the rest
public class InviteService(
    AppDbContext dbContext,
    IEmailService emailService,
    ILogger<InviteService> logger,
    string webBaseUrl = "http://localhost:3000") : IInviteService
{
    // At most this many open invitations per customer (no spamming the salons' calendars)
    public const int MaxWaitingPerCustomer = 3;

    // The others need at least this long to answer, otherwise the invitation cannot be sent
    public const int MinAnswerMinutes = 5;

    private readonly AvailabilityService availability = new(dbContext);
    private readonly BookingEmails emails = new(dbContext, emailService, logger, webBaseUrl);

    public async Task<InviteResult> CreateAsync(CreateInviteRequest request, string inviterUserId,
        CancellationToken cancellationToken = default)
    {
        var startUtc = ToUtc(request.StartUtc);
        if (!PhoneNumber.IsValid(request.Phone))
        {
            return Invalid("Oppgi et gyldig telefonnummer, f.eks. 912 34 567.");
        }

        if (request.Note is { Length: > 500 })
        {
            return Invalid("Meldingen kan være maks 500 tegn.");
        }

        if (!Enum.TryParse<PayMode>(request.PayMode, out var payMode) || !Enum.IsDefined(payMode))
        {
            return Invalid("Velg hvem som betaler.");
        }

        if (!InviteRules.DecisionMinutes.Contains(request.DecisionMinutes))
        {
            return Invalid("Velg en svarfrist fra listen.");
        }

        if (request.Persons is < InviteRules.MinPersons or > InviteRules.MaxPersons)
        {
            return Invalid($"Velg mellom {InviteRules.MinPersons} og {InviteRules.MaxPersons} personer.");
        }

        var nowUtc = DateTime.UtcNow;
        if (startUtc < BookingRules.EarliestStartUtc(nowUtc))
        {
            return Invalid("Velg en tid som er minst én time fram i tid.");
        }

        // Everyone must answer before the business's one-hour notice starts
        var expiresUtc = Min(nowUtc.AddMinutes(request.DecisionMinutes),
            startUtc.AddMinutes(-BookingRules.MinNoticeMinutes));
        if (expiresUtc < nowUtc.AddMinutes(MinAnswerMinutes))
        {
            return Invalid("Tiden er for nær. Velg en tid litt senere, så de andre rekker å svare.");
        }

        var service = await dbContext.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && !s.IsDeleted && !s.Business.IsHidden,
                cancellationToken);
        if (service is null)
        {
            return new InviteResult(InviteOutcome.NotFound);
        }

        if (AvailabilityService.VisitMinutes(service, request.Units) is not int minutes)
        {
            return Invalid("Velg en gyldig varighet.");
        }

        var waiting = await dbContext.BookingInvites.CountAsync(i => i.InviterUserId == inviterUserId
                                                                     && i.ClosedUtc == null
                                                                     && i.ExpiresUtc > nowUtc, cancellationToken);
        if (waiting >= MaxWaitingPerCustomer)
        {
            return Invalid($"Du har allerede {MaxWaitingPerCustomer} åpne invitasjoner. " +
                           "Vent på svar eller trekk en tilbake i Min Besti.");
        }

        // All places must be free now (nothing is held, but an invitation nobody can accept helps no one)
        var employees = await availability.GetFreeEmployeesAsync(service.Id, request.EmployeeId, startUtc,
            request.Units, request.Persons, cancellationToken);
        if (employees is null)
        {
            return new InviteResult(InviteOutcome.NotFound);
        }

        if (employees.Count < request.Persons)
        {
            return new InviteResult(InviteOutcome.SlotTaken);
        }

        if (request.OrganizerJoins
            && await CustomerCheckAsync(inviterUserId, service.BusinessId, startUtc, startUtc.AddMinutes(minutes),
                cancellationToken) is { } problem)
        {
            return problem;
        }

        var token = SecureToken.New();
        dbContext.BookingInvites.Add(new BookingInvite
        {
            Id = Guid.NewGuid(),
            TokenHash = SecureToken.Hash(token),
            InviterUserId = inviterUserId,
            InviterPhone = request.Phone.Trim(),
            InviterNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            BusinessId = service.BusinessId,
            ServiceId = service.Id,
            // The chosen employee is only for the inviter's own visit
            EmployeeId = request.OrganizerJoins ? request.EmployeeId : null,
            StartUtc = startUtc,
            Units = request.Units,
            Persons = request.Persons,
            OrganizerJoins = request.OrganizerJoins,
            PayMode = payMode,
            Status = InviteStatus.Venter,
            ExpiresUtc = expiresUtc,
            CreatedUtc = nowUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new InviteResult(InviteOutcome.Created, token, expiresUtc);
    }

    public async Task<InviteDto?> GetAsync(string token, string? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var invite = await FindByTokenAsync(token, cancellationToken);
        if (invite is null)
        {
            return null;
        }

        await CloseIfDueAsync(invite, DateTime.UtcNow, cancellationToken);

        var service = await dbContext.Services.AsNoTracking()
            .Where(s => s.Id == invite.ServiceId)
            .Select(s => new
            {
                s.Name, s.DurationMinutes, s.PriceNok, s.PriceType, BusinessName = s.Business.Name,
                s.Business.Slug, Address = s.Business.Address + ", " + s.Business.PostalCode + " " + s.Business.City
            })
            .FirstAsync(cancellationToken);
        var members = await MemberIdsAsync(invite.Id, cancellationToken);
        var names = await FirstNamesAsync([invite.InviterUserId, .. members], cancellationToken);
        var placesLeft = PlacesLeft(invite, members);
        var open = IsOpen(invite, members);
        var isMember = viewerUserId is not null && members.Contains(viewerUserId);

        // Can the visitor still join? Their place (and the inviter's before the first yes) must be free now
        var stillFree = open && (await availability.GetFreeEmployeesAsync(invite.ServiceId, null, invite.StartUtc,
            invite.Units, NeededPlaces(invite, members), cancellationToken))?.Count >= NeededPlaces(invite, members);

        return new InviteDto(
            invite.Id,
            invite.Status.ToString(),
            names.GetValueOrDefault(invite.InviterUserId, "Vennen din"),
            OrderedNames(invite, members, names),
            service.BusinessName,
            service.Slug,
            service.Address,
            service.Name,
            service.DurationMinutes * invite.Units,
            service.PriceNok,
            service.PriceType.ToString(),
            invite.StartUtc,
            invite.ExpiresUtc,
            invite.PayMode.ToString(),
            invite.Persons,
            invite.OrganizerJoins,
            members.Count,
            placesLeft,
            open,
            viewerUserId is not null && viewerUserId == invite.InviterUserId,
            isMember,
            stillFree);
    }

    public async Task<InviteResult> AcceptAsync(string token, string friendUserId, AcceptInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!PhoneNumber.IsValid(request.Phone))
        {
            return Invalid("Oppgi et gyldig telefonnummer, f.eks. 912 34 567.");
        }

        if (request.Note is { Length: > 500 })
        {
            return Invalid("Meldingen kan være maks 500 tegn.");
        }

        var invite = await FindByTokenAsync(token, cancellationToken);
        if (invite is null)
        {
            return new InviteResult(InviteOutcome.NotFound);
        }

        if (invite.InviterUserId == friendUserId)
        {
            return new InviteResult(InviteOutcome.OwnInvite);
        }

        // Two people clicking in the same second: one at a time. All employees of the business are locked too
        // (always in the same order, so two invitations never wait for each other), so nobody books in between
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null)
        {
            var employeeIds = await dbContext.Employees.Where(e => e.BusinessId == invite.BusinessId)
                .Select(e => e.Id).ToListAsync(cancellationToken);
            foreach (var name in new[] { $"invite-{invite.Id}" }
                         .Concat(employeeIds.Order().Select(id => $"booking-{id}")))
            {
                if (!await TryLockAsync(name, cancellationToken))
                {
                    return new InviteResult(InviteOutcome.SlotTaken);
                }
            }

            // Read again inside the lock: someone may have answered just before us
            await dbContext.Entry(invite).ReloadAsync(cancellationToken);
        }

        var nowUtc = DateTime.UtcNow;
        var members = await MemberIdsAsync(invite.Id, cancellationToken);
        if (members.Contains(friendUserId))
        {
            return new InviteResult(InviteOutcome.AlreadyJoined);
        }

        if (invite.Status is InviteStatus.Avslatt or InviteStatus.Trukket)
        {
            return new InviteResult(InviteOutcome.AlreadyAnswered);
        }

        if (PlacesLeft(invite, members) == 0)
        {
            return new InviteResult(InviteOutcome.Full);
        }

        if (invite.ExpiresUtc <= nowUtc)
        {
            await CloseIfDueAsync(invite, nowUtc, cancellationToken);
            await CommitAsync(transaction, cancellationToken);
            return new InviteResult(InviteOutcome.Expired);
        }

        var service = await dbContext.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == invite.ServiceId, cancellationToken);
        var inviter = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == invite.InviterUserId, cancellationToken);
        var friend = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == friendUserId, cancellationToken);
        if (service is null || inviter is null || friend is null
            || AvailabilityService.VisitMinutes(service, invite.Units) is not int minutes)
        {
            return new InviteResult(InviteOutcome.NotFound);
        }

        // The inviter's own visit is booked together with the first yes
        var withInviter = invite.OrganizerJoins && !members.Contains(invite.InviterUserId);
        var employees = await availability.GetFreeEmployeesAsync(invite.ServiceId,
            withInviter ? invite.EmployeeId : null, invite.StartUtc, invite.Units, withInviter ? 2 : 1,
            cancellationToken);
        if (employees is null || employees.Count < (withInviter ? 2 : 1))
        {
            return new InviteResult(InviteOutcome.SlotTaken);
        }

        var endUtc = invite.StartUtc.AddMinutes(minutes);
        // The friend must be free then and within the limit at this business; the inviter was checked when
        // sending, but may have booked something else at that time since
        if (await CustomerCheckAsync(friendUserId, invite.BusinessId, invite.StartUtc, endUtc,
                cancellationToken) is { } friendProblem)
        {
            return friendProblem;
        }

        if (withInviter && await CustomerCheckAsync(invite.InviterUserId, invite.BusinessId, invite.StartUtc,
                endUtc, cancellationToken) is not null)
        {
            return Invalid($"{FirstName(inviter.FullName)} har fått en annen time på dette tidspunktet. " +
                           "Be om en ny invitasjon.");
        }

        var autoConfirm = await dbContext.Businesses.Where(b => b.Id == invite.BusinessId)
            .Select(b => b.AutoConfirm).FirstAsync(cancellationToken);
        var newBookings = new List<Booking>();
        if (withInviter)
        {
            newBookings.Add(NewBooking(invite, employees[0], endUtc, inviter.FullName, inviter.Email,
                invite.InviterPhone, invite.InviterNote, invite.InviterUserId, autoConfirm, nowUtc,
                BookingRules.PriceFor(service, invite.Units)));
        }

        var friendBooking = NewBooking(invite, employees[^1], endUtc, friend.FullName, friend.Email,
            request.Phone.Trim(), request.Note, friendUserId, autoConfirm, nowUtc,
            BookingRules.PriceFor(service, invite.Units));
        newBookings.Add(friendBooking);
        dbContext.Bookings.AddRange(newBookings);

        if (invite.Status == InviteStatus.Venter)
        {
            invite.Status = InviteStatus.Akseptert;
            invite.AnsweredUtc = nowUtc;
        }

        var joined = members.Count + newBookings.Count;
        var friendName = FirstName(friend.FullName);
        var when = OsloTime.Format(invite.StartUtc);
        var count = invite.Persons > 2 ? $" ({joined} av {invite.Persons})" : "";
        AddNotification(invite.InviterUserId, null, $"{friendName} blir med! 👯{count} {service.Name} {when}",
            "/min-besti", withInviter ? newBookings[0].Id : friendBooking.Id);
        AddNotification(null, invite.BusinessId,
            $"👯 {friendBooking.CustomerName} blir med i gruppa til {inviter.FullName}{count}, {service.Name} {when}",
            BookingNotifier.PanelLink(friendBooking), friendBooking.Id);

        var full = joined >= invite.Persons;
        if (full)
        {
            invite.ClosedUtc = nowUtc;
            ForgetContactDetails(invite); // now on the bookings, no need to keep it twice
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Second line of defence: the unique index refused a booking for the same employee and time
            return new InviteResult(InviteOutcome.SlotTaken);
        }

        await CommitAsync(transaction, cancellationToken);

        // Booked: the mails go out even if the friend closes the page now. The business gets one e-mail
        // with the final count when the group closes, not one per person
        foreach (var booking in newBookings)
        {
            await emails.SendConfirmationAsync(booking, service, CancellationToken.None);
        }

        if (full)
        {
            await SendFinalCountAsync(invite, CancellationToken.None);
        }

        return new InviteResult(InviteOutcome.Accepted);
    }

    public async Task<InviteResult> DeclineAsync(string token, string? userId,
        CancellationToken cancellationToken = default)
    {
        var invite = await FindByTokenAsync(token, cancellationToken);
        if (invite is null)
        {
            return new InviteResult(InviteOutcome.NotFound);
        }

        if (userId is not null && userId == invite.InviterUserId)
        {
            return new InviteResult(InviteOutcome.OwnInvite);
        }

        if (await CloseIfDueAsync(invite, DateTime.UtcNow, cancellationToken))
        {
            return new InviteResult(InviteOutcome.Expired);
        }

        var members = await MemberIdsAsync(invite.Id, cancellationToken);
        if (invite.Status is InviteStatus.Avslatt or InviteStatus.Trukket || !IsOpen(invite, members))
        {
            return new InviteResult(InviteOutcome.AlreadyAnswered);
        }

        // Only one place for others: their "no" ends it. In a group it only tells the inviter
        if (OthersPlaces(invite) == 1)
        {
            invite.Status = InviteStatus.Avslatt;
            invite.AnsweredUtc = DateTime.UtcNow;
            invite.ClosedUtc = DateTime.UtcNow;
            ForgetContactDetails(invite);
        }

        var names = await FirstNamesAsync([userId], cancellationToken);
        var who = userId is null ? "En venn" : names.GetValueOrDefault(userId, "En venn");
        var serviceName = await dbContext.Services.Where(s => s.Id == invite.ServiceId).Select(s => s.Name)
            .FirstAsync(cancellationToken);
        AddNotification(invite.InviterUserId, null,
            $"{who} kan dessverre ikke denne gangen ({serviceName} {OsloTime.Format(invite.StartUtc)}).",
            "/min-besti", null);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new InviteResult(InviteOutcome.Declined);
    }

    public async Task<IReadOnlyList<CustomerInviteDto>> GetForCustomerAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var joinedIds = dbContext.Bookings
            .Where(b => b.CustomerUserId == userId && b.InviteId != null)
            .Select(b => b.InviteId!.Value);
        var invites = await dbContext.BookingInvites
            .Include(i => i.Service).ThenInclude(s => s.Business)
            .Where(i => i.InviterUserId == userId || joinedIds.Contains(i.Id))
            .OrderByDescending(i => i.CreatedUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;
        var result = new List<CustomerInviteDto>();
        foreach (var invite in invites)
        {
            await CloseIfDueAsync(invite, nowUtc, cancellationToken);
            var members = await MemberIdsAsync(invite.Id, cancellationToken);
            var names = await FirstNamesAsync([invite.InviterUserId, .. members], cancellationToken);
            result.Add(new CustomerInviteDto(
                invite.Id,
                invite.Status.ToString(),
                invite.InviterUserId == userId,
                names.GetValueOrDefault(invite.InviterUserId, "Vennen din"),
                OrderedNames(invite, members, names),
                invite.Service.Business.Name,
                invite.Service.Business.Slug,
                invite.Service.Name,
                invite.StartUtc,
                invite.ExpiresUtc,
                invite.PayMode.ToString(),
                invite.Persons,
                members.Count,
                IsOpen(invite, members)));
        }

        return result;
    }

    public async Task<InviteResult> WithdrawAsync(Guid inviteId, string inviterUserId,
        CancellationToken cancellationToken = default)
    {
        var invite = await dbContext.BookingInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InviterUserId == inviterUserId, cancellationToken);
        if (invite is null)
        {
            return new InviteResult(InviteOutcome.NotFound);
        }

        if (await CloseIfDueAsync(invite, DateTime.UtcNow, cancellationToken))
        {
            return new InviteResult(InviteOutcome.Expired);
        }

        // Once someone has said yes, their visit is booked: then it can only run out (or be cancelled in Min Besti)
        if (invite.Status != InviteStatus.Venter)
        {
            return new InviteResult(InviteOutcome.AlreadyAnswered);
        }

        invite.Status = InviteStatus.Trukket;
        invite.AnsweredUtc = DateTime.UtcNow;
        invite.ClosedUtc = DateTime.UtcNow;
        ForgetContactDetails(invite);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new InviteResult(InviteOutcome.Declined);
    }

    public async Task<InviteResult> NewLinkAsync(Guid inviteId, string inviterUserId,
        CancellationToken cancellationToken = default)
    {
        var invite = await dbContext.BookingInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InviterUserId == inviterUserId, cancellationToken);
        if (invite is null)
        {
            return new InviteResult(InviteOutcome.NotFound);
        }

        if (await CloseIfDueAsync(invite, DateTime.UtcNow, cancellationToken))
        {
            return new InviteResult(InviteOutcome.Expired);
        }

        var members = await MemberIdsAsync(invite.Id, cancellationToken);
        if (!IsOpen(invite, members))
        {
            return new InviteResult(InviteOutcome.AlreadyAnswered);
        }

        var token = SecureToken.New();
        invite.TokenHash = SecureToken.Hash(token);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new InviteResult(InviteOutcome.Created, token, invite.ExpiresUtc);
    }

    public async Task<int> CloseDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var due = await dbContext.BookingInvites
            .Where(i => i.ClosedUtc == null && i.ExpiresUtc <= nowUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
        foreach (var invite in due)
        {
            await CloseIfDueAsync(invite, nowUtc, cancellationToken);
        }

        return due.Count;
    }

    // The answer time is up: nobody said yes = Utlopt (the inviter is told); some did = the group is closed
    // with who joined, and the business gets the final count. Only once. Returns true when it is closed by time
    private async Task<bool> CloseIfDueAsync(BookingInvite invite, DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (invite.Status == InviteStatus.Utlopt)
        {
            return true;
        }

        if (invite.ClosedUtc is not null || invite.ExpiresUtc > nowUtc)
        {
            return false;
        }

        // The job, a visitor reading the link and a late "Jeg blir med!" can all get here in the same second:
        // only the one that sets ClosedUtc first in the database sends the messages
        if (dbContext.Database.IsRelational())
        {
            var claimed = await dbContext.BookingInvites
                .Where(i => i.Id == invite.Id && i.ClosedUtc == null)
                .ExecuteUpdateAsync(i => i.SetProperty(x => x.ClosedUtc, nowUtc), cancellationToken);
            if (claimed == 0)
            {
                await dbContext.Entry(invite).ReloadAsync(cancellationToken);
                return invite.Status == InviteStatus.Utlopt;
            }
        }

        invite.ClosedUtc = nowUtc;
        ForgetContactDetails(invite);
        if (invite.Status == InviteStatus.Venter)
        {
            invite.Status = InviteStatus.Utlopt;
            AddNotification(invite.InviterUserId, null,
                $"Ingen svar på invitasjonen for {OsloTime.Format(invite.StartUtc)}. Ingenting ble bestilt.",
                "/min-besti", null);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await SendFinalCountAsync(invite, CancellationToken.None);
        return false;
    }

    // "Gruppa er klar": who comes, to the business (bell and e-mail) and the inviter (bell)
    private async Task SendFinalCountAsync(BookingInvite invite, CancellationToken cancellationToken)
    {
        try
        {
            var bookings = await dbContext.Bookings.AsNoTracking()
                .Where(b => b.InviteId == invite.Id && b.Status != BookingStatus.Avlyst)
                .OrderBy(b => b.CreatedUtc)
                .Select(b => new { b.Id, b.CustomerName, b.CustomerPhone, b.StartUtc, b.ServiceId })
                .ToListAsync(cancellationToken);
            if (bookings.Count == 0)
            {
                return;
            }

            var service = await dbContext.Services.AsNoTracking()
                .Where(s => s.Id == invite.ServiceId).Select(s => s.Name).FirstAsync(cancellationToken);
            var inviterName = await dbContext.Users.AsNoTracking()
                .Where(u => u.Id == invite.InviterUserId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken);
            var when = OsloTime.Format(invite.StartUtc);
            var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(invite.StartUtc, SlotCalculator.Oslo));
            var link = $"/panel?dag={day:yyyy-MM-dd}&bestilling={bookings[0].Id}";
            var people = bookings.Count == 1 ? "1 person" : $"{bookings.Count} personer";
            AddNotification(null, invite.BusinessId, $"👯 Gruppa til {inviterName} er klar: {people}, {service} {when}",
                link, bookings[0].Id);
            AddNotification(invite.InviterUserId, null, $"Gruppa er klar 🎉 {people} kommer, {service} {when}",
                "/min-besti", null);
            await dbContext.SaveChangesAsync(cancellationToken);

            var ownerEmail = await dbContext.Users
                .Where(u => u.BusinessId == invite.BusinessId && u.EmailConfirmed)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(cancellationToken);
            if (ownerEmail is null)
            {
                return;
            }

            var body =
                "Hei!\n\n" +
                $"Gruppa til {inviterName} er klar (BESTI sammen 👯).\n\n" +
                $"Tjeneste: {service}\n" +
                $"Tid: {when}\n" +
                $"Antall: {people}\n" +
                (invite.PayMode == PayMode.JegSpanderer ? $"{inviterName} betaler for hele gruppa.\n" : "") +
                "\n" + string.Join("\n", bookings.Select(b => $"- {b.CustomerName}, tlf. {b.CustomerPhone}")) +
                $"\n\nSe gruppa i kalenderen: {webBaseUrl}{link}\n\n" +
                "Hilsen BESTI";
            await emailService.SendAsync(ownerEmail, $"Gruppe klar: {people}, {service} {when}", body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // The group is booked anyway; a failed message is only logged
            logger.LogWarning(ex, "Could not send the final count for invite {InviteId}", invite.Id);
        }
    }

    // Everyone booked from this invitation (cancelled visits do not count)
    private async Task<List<string>> MemberIdsAsync(Guid inviteId, CancellationToken cancellationToken) =>
        await dbContext.Bookings
            .Where(b => b.InviteId == inviteId && b.Status != BookingStatus.Avlyst && b.CustomerUserId != null)
            .OrderBy(b => b.CreatedUtc)
            .Select(b => b.CustomerUserId!)
            .ToListAsync(cancellationToken);

    // Places for the others (the inviter's own place is not theirs to take)
    private static int OthersPlaces(BookingInvite invite) => invite.Persons - (invite.OrganizerJoins ? 1 : 0);

    private static int PlacesLeft(BookingInvite invite, List<string> members) =>
        Math.Max(OthersPlaces(invite) - members.Count(id => id != invite.InviterUserId), 0);

    // Places one more "yes" needs right now: their own, and the inviter's before the first yes
    private static int NeededPlaces(BookingInvite invite, List<string> members) =>
        invite.OrganizerJoins && !members.Contains(invite.InviterUserId) ? 2 : 1;

    private static bool IsOpen(BookingInvite invite, List<string> members) =>
        invite.ClosedUtc is null
        && invite.Status is InviteStatus.Venter or InviteStatus.Akseptert
        && invite.ExpiresUtc > DateTime.UtcNow
        && PlacesLeft(invite, members) > 0;

    // The inviter first (when they come), then the others in the order they said yes
    private static IReadOnlyList<string> OrderedNames(BookingInvite invite, List<string> members,
        Dictionary<string, string> names) =>
        members.OrderBy(id => id == invite.InviterUserId ? 0 : 1)
            .Select(id => names.GetValueOrDefault(id, "Gjest"))
            .ToList();

    private async Task<BookingInvite?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
        {
            return null;
        }

        var hash = SecureToken.Hash(token);
        return await dbContext.BookingInvites.FirstOrDefaultAsync(i => i.TokenHash == hash, cancellationToken);
    }

    // The same rules as a normal booking: one visit at a time per customer, and a limit per business
    private async Task<InviteResult?> CustomerCheckAsync(string userId, Guid businessId, DateTime startUtc,
        DateTime endUtc, CancellationToken cancellationToken)
    {
        var busy = await dbContext.Bookings.AnyAsync(b => b.CustomerUserId == userId
                                                          && b.Status != BookingStatus.Avlyst
                                                          && b.StartUtc < endUtc
                                                          && b.EndUtc > startUtc, cancellationToken);
        if (busy)
        {
            return new InviteResult(InviteOutcome.CustomerBusy);
        }

        var nowUtc = DateTime.UtcNow;
        var upcoming = await dbContext.Bookings.CountAsync(b => b.CustomerUserId == userId
                                                                && b.Service.BusinessId == businessId
                                                                && b.Status != BookingStatus.Avlyst
                                                                && b.StartUtc > nowUtc, cancellationToken);
        return upcoming >= BookingRules.MaxUpcomingPerBusiness
            ? Invalid($"Du har allerede {BookingRules.MaxUpcomingPerBusiness} kommende timer hos denne bedriften. " +
                      "Avbestill en av dem i Min Besti, eller ring bedriften.")
            : null;
    }

    private static Booking NewBooking(BookingInvite invite, Guid employeeId, DateTime endUtc, string? name,
        string? email, string phone, string? note, string userId, bool autoConfirm, DateTime nowUtc,
        decimal? priceNok) => new()
    {
        Id = Guid.NewGuid(),
        ServiceId = invite.ServiceId,
        EmployeeId = employeeId,
        StartUtc = invite.StartUtc,
        EndUtc = endUtc,
        CustomerName = (name ?? "").Trim(),
        CustomerPhone = phone,
        CustomerEmail = (email ?? "").Trim().ToLowerInvariant(),
        CustomerNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        CustomerUserId = userId,
        Status = autoConfirm ? BookingStatus.Bekreftet : BookingStatus.Venter,
        CreatedUtc = nowUtc,
        InviteId = invite.Id,
        PriceNok = priceNok
    };

    // Phone and message are only needed until the inviter's own visit is booked or the group is closed
    private static void ForgetContactDetails(BookingInvite invite)
    {
        invite.InviterPhone = "-";
        invite.InviterNote = null;
    }

    // A message in the bell of a customer (userId) or a business (businessId)
    private void AddNotification(string? userId, Guid? businessId, string text, string link, Guid? bookingId) =>
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessId = businessId,
            BookingId = bookingId,
            Text = text.Length <= 300 ? text : text[..299] + "…",
            Link = link,
            CreatedUtc = DateTime.UtcNow
        });

    private async Task<Dictionary<string, string>> FirstNamesAsync(IReadOnlyList<string?> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.OfType<string>().Distinct().ToList();
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(cancellationToken);
        return users.ToDictionary(u => u.Id, u => FirstName(u.FullName));
    }

    // Only the first name is shown to someone with the link ("Kari inviterer deg")
    public static string FirstName(string? fullName) =>
        fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Vennen din";

    private static InviteResult Invalid(string error) => new(InviteOutcome.Invalid, Error: error);

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static async Task CommitAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    // Same SQL Server lock as BookingService uses for one employee ("booking-{employeeId}")
    private async Task<bool> TryLockAsync(string lockName, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DECLARE @result int; EXEC @result = sp_getapplock @Resource = {lockName}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Booking lock not granted', 1;",
                cancellationToken);
            return true;
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            return false;
        }
    }

    // "2026-10-22T10:00:00+02:00" arrives as local time; a time without a zone is taken as UTC
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };
}
