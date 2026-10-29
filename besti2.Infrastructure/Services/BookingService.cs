using besti2.Application.Bookings;
using besti2.Application.Common;
using besti2.Application.Email;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

// webBaseUrl = address of the Next.js site, used for the links in the e-mails
public class BookingService(
    AppDbContext dbContext,
    IEmailService emailService,
    ILogger<BookingService> logger,
    string webBaseUrl = "http://localhost:3000") : IBookingService
{
    private readonly BookingEmails emails = new(dbContext, emailService, logger, webBaseUrl);
    private readonly AvailabilityService availability = new(dbContext);
    private readonly BookingNotifier notifier = new(dbContext, logger);

    private static readonly TimeZoneInfo Oslo = SlotCalculator.Oslo;

    public async Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, string? customerUserId = null,
        CancellationToken cancellationToken = default)
    {
        // "2026-10-22T10:00:00+02:00" arrives as local time: turn every start into real UTC first
        request = request with { StartUtc = ToUtc(request.StartUtc) };

        var error = Validate(request);
        if (error is not null)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid, Error: error);
        }

        // The salon must have time to see it (the free times on the page already start an hour from now)
        if (request.StartUtc < BookingRules.EarliestStartUtc(DateTime.UtcNow))
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid,
                Error: "Velg en tid som er minst én time fram i tid.");
        }

        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && !s.IsDeleted, cancellationToken);
        if (service is null)
        {
            return new CreateBookingResult(CreateBookingOutcome.NotFound);
        }

        if (AvailabilityService.VisitMinutes(service, request.Units) is not int minutes)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid, Error: "Velg en gyldig varighet.");
        }

        // The day in Oslo, not in UTC (00:30 on Tuesday in Oslo is still Monday in UTC)
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(request.StartUtc, Oslo));

        // Only one booking per employee is checked and saved at a time, so two clicks in the same
        // second cannot both get the slot (SQL Server lock; the in-memory test database has no locks)
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null && !await TryLockEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            // Someone else is booking this employee right now: the customer simply tries another time
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        var freeSlots = await availability.GetFreeSlotsAsync(request.ServiceId, request.EmployeeId, date, request.Units,
            cancellationToken);
        if (freeSlots is null)
        {
            return new CreateBookingResult(CreateBookingOutcome.NotFound);
        }

        if (!freeSlots.Contains(request.StartUtc))
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        // The same customer account cannot have two visits at the same time (any business);
        // booking for someone else comes later with BESTI sammen
        if (customerUserId is not null)
        {
            var endUtc = request.StartUtc.AddMinutes(minutes);
            var customerBusy = await dbContext.Bookings.AnyAsync(b =>
                b.CustomerUserId == customerUserId
                && b.Status != BookingStatus.Avlyst
                && b.StartUtc < endUtc
                && b.EndUtc > request.StartUtc, cancellationToken);
            if (customerBusy)
            {
                return new CreateBookingResult(CreateBookingOutcome.CustomerBusy);
            }

            // One account cannot fill the calendar of a salon
            var nowUtc = DateTime.UtcNow;
            var upcoming = await dbContext.Bookings.CountAsync(b =>
                b.CustomerUserId == customerUserId
                && b.Service.BusinessId == service.BusinessId
                && b.Status != BookingStatus.Avlyst
                && b.StartUtc > nowUtc, cancellationToken);
            if (upcoming >= BookingRules.MaxUpcomingPerBusiness)
            {
                return new CreateBookingResult(CreateBookingOutcome.Invalid,
                    Error: $"Du har allerede {BookingRules.MaxUpcomingPerBusiness} kommende timer hos denne bedriften. " +
                           "Avbestill en av dem i Min Besti, eller ring bedriften.");
            }
        }

        // The business may confirm new bookings automatically (Panel → Profil)
        var autoConfirm = await dbContext.Businesses
            .Where(b => b.Id == service.BusinessId)
            .Select(b => b.AutoConfirm)
            .FirstAsync(cancellationToken);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            EmployeeId = request.EmployeeId,
            StartUtc = request.StartUtc,
            EndUtc = request.StartUtc.AddMinutes(minutes),
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail.Trim().ToLowerInvariant(),
            MarketingConsent = request.MarketingConsent,
            MarketingConsentUtc = request.MarketingConsent ? DateTime.UtcNow : null,
            CustomerNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CustomerUserId = customerUserId,
            Status = autoConfirm ? BookingStatus.Bekreftet : BookingStatus.Venter,
            CreatedUtc = DateTime.UtcNow,
            PriceNok = BookingRules.PriceFor(service, request.Units)
        };

        dbContext.Bookings.Add(booking);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Second line of defence: the unique index refused a second booking for the same time
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        // The booking is saved: mails and the bell must go out even if the customer closes the page now
        // (with the request's token a closed page cancelled the mail to the business). Same everywhere below.
        await emails.SendConfirmationAsync(booking, service, CancellationToken.None);
        await emails.SendNewBookingToOwnerAsync(booking, service, BookingNotifier.PanelLink(booking),
            CancellationToken.None);
        await notifier.NotifyAsync(booking, toBusiness: true,
            $"Ny bestilling: {booking.CustomerName}, {service.Name} {OsloTime.Format(booking.StartUtc)}",
            BookingNotifier.PanelLink(booking),
            CancellationToken.None);

        return new CreateBookingResult(
            CreateBookingOutcome.Created,
            new BookingCreatedDto(booking.Id, booking.StartUtc, booking.EndUtc, booking.Status.ToString()));
    }

    // The business adds a visit itself (phone call, walk-in). Confirmed at once; it may be outside the
    // opening hours, but never on top of another visit of the same employee
    public async Task<CreateBookingResult> CreateByBusinessAsync(Guid businessId, CreateManualBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateManual(request);
        if (error is not null)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid, Error: error);
        }

        // Service and employee must belong to this business
        var service = await dbContext.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.BusinessId == businessId && !s.IsDeleted,
                cancellationToken);
        var employeeExists = await dbContext.Employees.AnyAsync(
            e => e.Id == request.EmployeeId && e.BusinessId == businessId && !e.IsDeleted, cancellationToken);
        if (service is null || !employeeExists)
        {
            return new CreateBookingResult(CreateBookingOutcome.NotFound);
        }

        if (AvailabilityService.VisitMinutes(service, request.Units) is not int minutes)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid, Error: "Velg en gyldig varighet.");
        }

        var startUtc = SlotCalculator.ToUtc(request.Date, request.Time);
        var endUtc = startUtc.AddMinutes(minutes);

        // Same protection as online bookings: one booking per employee is checked and saved at a time
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null && !await TryLockEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        // Blocked time (holiday, closed day) cannot be booked, not even by the business itself
        var blocked = await dbContext.TimeOffs.AnyAsync(t => t.BusinessId == businessId
                                                             && (t.EmployeeId == null ||
                                                                 t.EmployeeId == request.EmployeeId)
                                                             && t.StartUtc < endUtc && t.EndUtc > startUtc,
            cancellationToken);
        if (blocked)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid,
                Error: "Den ansatte er ikke tilgjengelig da (blokkert tid).");
        }

        var overlaps = await dbContext.Bookings.AnyAsync(b => b.EmployeeId == request.EmployeeId
                                                              && b.Status != BookingStatus.Avlyst
                                                              && b.StartUtc < endUtc && b.EndUtc > startUtc,
            cancellationToken);
        if (overlaps)
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        var email = request.CustomerEmail?.Trim().ToLowerInvariant() ?? "";
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            EmployeeId = request.EmployeeId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = email,
            CustomerNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = BookingStatus.Bekreftet,
            CreatedUtc = DateTime.UtcNow,
            NoReminder = !request.SendConfirmation,
            PriceNok = BookingRules.PriceFor(service, request.Units)
        };
        dbContext.Bookings.Add(booking);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        // Only when the business asked for it and gave an e-mail address
        if (request.SendConfirmation && email.Length > 0)
        {
            await emails.SendStatusAsync(booking, null, CancellationToken.None);
        }

        return new CreateBookingResult(
            CreateBookingOutcome.Created,
            new BookingCreatedDto(booking.Id, booking.StartUtc, booking.EndUtc, booking.Status.ToString()));
    }

    public async Task<IReadOnlyList<PanelBookingDto>> GetForBusinessAsync(
        Guid businessId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        // Calendar: from midnight of the first day in Oslo. List: upcoming bookings and the last 14 days
        var fromUtc = from is DateOnly firstDay
            ? SlotCalculator.ToUtc(firstDay, TimeOnly.MinValue)
            : DateTime.UtcNow.AddDays(-14);
        // Visits are max 8 hours: older ones cannot reach into the period (keeps the index read short)
        var startsAfterUtc = fromUtc.AddMinutes(-BookingRules.MaxVisitMinutes);
        var query = dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.StartUtc > startsAfterUtc && b.EndUtc > fromUtc);
        if (to is DateOnly lastDay)
        {
            // ... until midnight after the last day (a visit is shown on every day it touches)
            var toUtc = SlotCalculator.ToUtc(lastDay.AddDays(1), TimeOnly.MinValue);
            query = query.Where(b => b.StartUtc < toUtc);
        }

        var bookings = await query
            .OrderBy(b => b.StartUtc)
            .Select(b => new PanelBookingDto(
                b.Id,
                b.StartUtc,
                b.EndUtc,
                b.Status.ToString(),
                b.Service.Name,
                b.Employee.Name + " " + b.Employee.LastName,
                b.CustomerName,
                b.CustomerPhone,
                b.CustomerEmail,
                b.CustomerNote,
                b.EmployeeId,
                b.PreviousStartUtc,
                0,
                b.InviteId))
            .ToListAsync(cancellationToken);
        bookings = await AddGroupsAsync(bookings, cancellationToken);

        // "Møtte ikke" for these customers at this business only (other businesses never see it), counted in
        // ONE query for the whole week. Deleted and anonymized customers all share one address: never add them up
        var emails = bookings.Select(b => b.CustomerEmail)
            .Where(email => email != "" && email != BookingAnonymizer.AnonymizedEmail)
            .Distinct()
            .ToList();
        if (emails.Count == 0)
        {
            return bookings;
        }

        var noShows = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.MotteIkke
                        && b.Service.BusinessId == businessId
                        && emails.Contains(b.CustomerEmail))
            .GroupBy(b => b.CustomerEmail)
            .Select(g => new { Email = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Email, x => x.Count, cancellationToken);
        return bookings
            .Select(b => noShows.TryGetValue(b.CustomerEmail, out var count) ? b with { NoShowCount = count } : b)
            .ToList();
    }

    // BESTI sammen: the whole group for every visit that belongs to one (one query for the invitations,
    // one for their visits, also those outside the shown days)
    private async Task<List<PanelBookingDto>> AddGroupsAsync(List<PanelBookingDto> bookings,
        CancellationToken cancellationToken)
    {
        var inviteIds = bookings.Where(b => b.InviteId != null).Select(b => b.InviteId!.Value).Distinct().ToList();
        if (inviteIds.Count == 0)
        {
            return bookings;
        }

        var invites = await dbContext.BookingInvites
            .AsNoTracking()
            .Where(i => inviteIds.Contains(i.Id))
            .Select(i => new
            {
                i.Id, i.Persons, i.ExpiresUtc, i.ClosedUtc, i.Status, i.OrganizerJoins, i.PayMode,
                i.InviterUserId, OrganizerName = dbContext.Users.Where(u => u.Id == i.InviterUserId)
                    .Select(u => u.FullName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        var members = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.InviteId != null && inviteIds.Contains(b.InviteId.Value) && b.Status != BookingStatus.Avlyst)
            .OrderBy(b => b.CreatedUtc)
            .Select(b => new { b.Id, InviteId = b.InviteId!.Value, b.CustomerName, b.CustomerUserId })
            .ToListAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;
        var groups = invites.ToDictionary(i => i.Id, i =>
        {
            var inGroup = members.Where(m => m.InviteId == i.Id).ToList();
            var organizerFirst = inGroup.OrderBy(m => m.CustomerUserId == i.InviterUserId ? 0 : 1).ToList();
            return (Invite: i, Members: organizerFirst,
                Open: i.ClosedUtc == null && i.ExpiresUtc > nowUtc && inGroup.Count < i.Persons
                      && i.Status is InviteStatus.Venter or InviteStatus.Akseptert);
        });

        return bookings.Select(b =>
        {
            if (b.InviteId is not Guid id || !groups.TryGetValue(id, out var group))
            {
                return b;
            }

            var invite = group.Invite;
            var isOrganizer = group.Members.Any(m => m.Id == b.Id && m.CustomerUserId == invite.InviterUserId);
            return b with
            {
                Group = new PanelGroupDto(id, invite.Persons, group.Members.Count, invite.ExpiresUtc, group.Open,
                    invite.OrganizerName ?? "Organisator", invite.OrganizerJoins,
                    invite.PayMode == PayMode.JegSpanderer, isOrganizer,
                    group.Members.Select(m => m.CustomerName).ToList())
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<CustomerBookingDto>> GetForCustomerAsync(
        string customerUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.CustomerUserId == customerUserId)
            .OrderByDescending(b => b.StartUtc)
            .Select(b => new CustomerBookingDto(
                b.Id,
                b.StartUtc,
                b.EndUtc,
                b.Status.ToString(),
                b.Service.Name,
                b.Employee.Name + " " + b.Employee.LastName,
                b.Service.BusinessId,
                b.Service.Business.Name,
                b.Service.Business.Slug,
                b.Service.Business.Address + ", " + b.Service.Business.PostalCode + " " + b.Service.Business.City,
                b.ServiceId,
                b.EmployeeId,
                dbContext.Reviews.Any(r => r.BookingId == b.Id),
                b.Service.Business.PhoneNumber,
                b.Service.Business.FreeCancelHours,
                b.PreviousStartUtc,
                b.InviteId))
            .ToListAsync(cancellationToken);
    }

    public async Task<UpdateBookingStatusOutcome> CancelByCustomerAsync(
        Guid bookingId, string customerUserId, string? message = null, bool ignoreDeadline = false,
        CancellationToken cancellationToken = default)
    {
        // Only the customer's own booking; someone else's id looks like "not found"
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerUserId == customerUserId, cancellationToken);
        if (booking is null)
        {
            return UpdateBookingStatusOutcome.NotFound;
        }

        // Already cancelled or already started: too late to cancel
        if (booking.Status == BookingStatus.Avlyst || booking.StartUtc <= DateTime.UtcNow)
        {
            return UpdateBookingStatusOutcome.InvalidTransition;
        }

        // A confirmed visit: after the business's free-cancel deadline only by phone
        // (a visit still waiting for the business can always be withdrawn)
        if (!ignoreDeadline && booking.Status == BookingStatus.Bekreftet)
        {
            var freeCancelHours = await dbContext.Services
                .Where(s => s.Id == booking.ServiceId)
                .Select(s => s.Business.FreeCancelHours)
                .FirstAsync(cancellationToken);
            if (DateTime.UtcNow > booking.StartUtc.AddHours(-freeCancelHours))
            {
                return UpdateBookingStatusOutcome.TooLate;
            }
        }

        booking.Status = BookingStatus.Avlyst;
        booking.NewTimeTokenHash = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        // The customer's optional reason goes to the business in the e-mail and the bell
        var note = ShortNote(message);
        await emails.SendCancelledByCustomerAsync(booking, note, CancellationToken.None);
        await notifier.NotifyAsync(booking, toBusiness: true,
            $"{booking.CustomerName} har avbestilt timen {OsloTime.Format(booking.StartUtc)}" +
            (note is null ? "" : $": {note}"),
            BookingNotifier.PanelLink(booking), CancellationToken.None);
        return UpdateBookingStatusOutcome.Updated;
    }

    // Update the status of a booking
    public async Task<UpdateBookingStatusOutcome> UpdateStatusAsync(
        Guid bookingId, Guid businessId, BookingStatus newStatus, string? message = null,
        CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Service.BusinessId == businessId, cancellationToken);

        if (booking is null)
        {
            return UpdateBookingStatusOutcome.NotFound;
        }

        var started = booking.StartUtc <= DateTime.UtcNow;
        var allowed = (booking.Status, newStatus) switch
        {
            (BookingStatus.Venter, BookingStatus.Bekreftet) => true,
            // "Møtte ikke" only after the start time; "Møtte" (Bekreftet) undoes a mistake
            (BookingStatus.Venter or BookingStatus.Bekreftet, BookingStatus.MotteIkke) => started,
            (BookingStatus.MotteIkke, BookingStatus.Bekreftet) => true,
            // A moved visit the customer never answered: after the start the business marks whether they came
            (BookingStatus.VenterKunde, BookingStatus.Bekreftet or BookingStatus.MotteIkke) => started,
            (BookingStatus.Venter, BookingStatus.Avlyst) => true,
            (BookingStatus.Bekreftet, BookingStatus.Avlyst) => true,
            (BookingStatus.VenterKunde, BookingStatus.Avlyst) => true,
            _ => false
        };

        if (!allowed)
        {
            return UpdateBookingStatusOutcome.InvalidTransition;
        }

        // A visit that has started can still be confirmed, but no longer cancelled
        // (otherwise a business could cancel past visits to block reviews)
        if (newStatus == BookingStatus.Avlyst && booking.StartUtc <= DateTime.UtcNow)
        {
            return UpdateBookingStatusOutcome.InvalidTransition;
        }

        var wasNoShow = booking.Status == BookingStatus.MotteIkke;
        booking.Status = newStatus;
        booking.NewTimeTokenHash = null;
        booking.PreviousStartUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        // After the start time the business only marks whether the customer came
        if (started)
        {
            await NotifyNoShowAsync(booking, wasNoShow, CancellationToken.None);
            return UpdateBookingStatusOutcome.Updated;
        }

        // A short note from the business, only when cancelling
        var note = newStatus == BookingStatus.Avlyst ? ShortNote(message) : null;
        await emails.SendStatusAsync(booking, note, CancellationToken.None);
        await notifier.NotifyAsync(booking, toBusiness: false,
            newStatus == BookingStatus.Bekreftet
                ? $"{{0}} har bekreftet timen {OsloTime.Format(booking.StartUtc)}"
                : $"{{0}} har avlyst timen {OsloTime.Format(booking.StartUtc)}" + (note is null ? "" : $": {note}"),
            newStatus == BookingStatus.Bekreftet ? "/min-besti" : null, CancellationToken.None);
        return UpdateBookingStatusOutcome.Updated;
    }

    // "Flytt": another time and/or employee; never on top of another visit or blocked time,
    // outside the opening hours is allowed (as for visits the business adds itself)
    public async Task<MoveBookingResult> MoveAsync(Guid bookingId, Guid businessId, MoveBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Service.BusinessId == businessId, cancellationToken);
        var employeeExists = await dbContext.Employees.AnyAsync(
            e => e.Id == request.EmployeeId && e.BusinessId == businessId && !e.IsDeleted, cancellationToken);
        if (booking is null || !employeeExists)
        {
            return new MoveBookingResult(MoveBookingOutcome.NotFound);
        }

        var nowUtc = DateTime.UtcNow;
        if (booking.Status == BookingStatus.Avlyst || booking.StartUtc <= nowUtc)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid,
                "Timen er avlyst eller har allerede startet, og kan ikke flyttes.");
        }

        if (request.Date.Year is < 2000 or > 2100)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid, "Ugyldig dato.");
        }

        // Same length as before (also when the service was changed since)
        var startUtc = SlotCalculator.ToUtc(request.Date, request.Time);
        var endUtc = startUtc + (booking.EndUtc - booking.StartUtc);
        if (startUtc <= nowUtc)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid, "Velg et tidspunkt fram i tid.");
        }

        if (startUtc == booking.StartUtc && request.EmployeeId == booking.EmployeeId)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid, "Velg en ny tid eller en annen ansatt.");
        }

        // The same lock as new bookings, for the employee who gets the visit
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null && !await TryLockEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        var blocked = await dbContext.TimeOffs.AnyAsync(t => t.BusinessId == businessId
                                                             && (t.EmployeeId == null ||
                                                                 t.EmployeeId == request.EmployeeId)
                                                             && t.StartUtc < endUtc && t.EndUtc > startUtc,
            cancellationToken);
        if (blocked)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid,
                "Den ansatte er ikke tilgjengelig da (blokkert tid).");
        }

        // Another visit of that employee (the moved visit itself does not count: 10:00 -> 10:15 is fine)
        var overlaps = await dbContext.Bookings.AnyAsync(b => b.Id != booking.Id
                                                              && b.EmployeeId == request.EmployeeId
                                                              && b.Status != BookingStatus.Avlyst
                                                              && b.StartUtc < endUtc && b.EndUtc > startUtc,
            cancellationToken);
        if (overlaps)
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        // The original time stays the "before" time when a visit is moved twice before the customer answers
        var oldStartUtc = booking.PreviousStartUtc ?? booking.StartUtc;
        booking.EmployeeId = request.EmployeeId;
        booking.StartUtc = startUtc;
        booking.EndUtc = endUtc;
        // A new time gets its own reminder
        booking.ReminderSentUtc = null;
        booking.ReminderAttempts = 0;
        // Ask the customer only when they can get the link (a walk-in without e-mail cannot answer)
        string? token = null;
        if (request.AskCustomer && booking.CustomerEmail.Length > 0)
        {
            token = SecureToken.New();
            booking.NewTimeTokenHash = SecureToken.Hash(token);
            booking.PreviousStartUtc = oldStartUtc;
            booking.Status = BookingStatus.VenterKunde;
        }
        else
        {
            booking.NewTimeTokenHash = null;
            booking.PreviousStartUtc = null;
            if (booking.Status == BookingStatus.VenterKunde)
            {
                booking.Status = BookingStatus.Venter;
            }
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        if (token is not null)
        {
            await emails.SendMovedAsync(booking, oldStartUtc, token, CancellationToken.None);
            await notifier.NotifyAsync(booking, toBusiness: false,
                $"{{0}} ønsker å flytte timen til {OsloTime.Format(booking.StartUtc)}. Passer det?", "/min-besti",
                CancellationToken.None);
        }
        else if (request.NotifyCustomer)
        {
            await emails.SendMovedAsync(booking, oldStartUtc, null, CancellationToken.None);
            await notifier.NotifyAsync(booking, toBusiness: false,
                $"{{0}} har flyttet timen til {OsloTime.Format(booking.StartUtc)}", "/min-besti", CancellationToken.None);
        }

        return new MoveBookingResult(MoveBookingOutcome.Moved);
    }

    public async Task<NewTimeDto?> GetNewTimeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var hash = SecureToken.Hash(token);
        return await dbContext.Bookings.AsNoTracking()
            .Where(b => b.NewTimeTokenHash == hash && b.Status == BookingStatus.VenterKunde)
            .Select(b => new NewTimeDto(b.Service.Business.Name, b.Service.Name,
                b.Employee.Name + " " + b.Employee.LastName, b.StartUtc, b.PreviousStartUtc, b.Status.ToString()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<NewTimeOutcome> AnswerNewTimeAsync(string token, bool accept,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return NewTimeOutcome.NotFound;
        }

        var hash = SecureToken.Hash(token);
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.NewTimeTokenHash == hash, cancellationToken);
        return await AnswerAsync(booking, accept, cancellationToken);
    }

    public async Task<NewTimeOutcome> AnswerNewTimeByCustomerAsync(Guid bookingId, string customerUserId, bool accept,
        CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerUserId == customerUserId, cancellationToken);
        return await AnswerAsync(booking, accept, cancellationToken);
    }

    // Godta = the visit waits for the business to confirm, as a new booking; Passer ikke = cancelled and free again
    private async Task<NewTimeOutcome> AnswerAsync(Booking? booking, bool accept, CancellationToken cancellationToken)
    {
        if (booking is null || booking.Status != BookingStatus.VenterKunde)
        {
            return NewTimeOutcome.NotFound;
        }

        if (booking.StartUtc <= DateTime.UtcNow)
        {
            return NewTimeOutcome.TooLate;
        }

        // Accepted: confirmed at once when the business confirms new bookings automatically
        var autoConfirm = await dbContext.Services
            .Where(s => s.Id == booking.ServiceId)
            .Select(s => s.Business.AutoConfirm)
            .FirstAsync(cancellationToken);
        booking.Status = !accept ? BookingStatus.Avlyst
            : autoConfirm ? BookingStatus.Bekreftet
            : BookingStatus.Venter;
        booking.NewTimeTokenHash = null;
        booking.PreviousStartUtc = null;
        // Waits for "Bekreft" again: the business may be reminded again
        booking.PendingReminderSentUtc = null;
        // The question "Passer den nye tiden?" is answered: it leaves the customer's bell
        if (booking.CustomerUserId is not null)
        {
            dbContext.Notifications.RemoveRange(await dbContext.Notifications
                .Where(n => n.UserId == booking.CustomerUserId && n.BookingId == booking.Id)
                .ToListAsync(cancellationToken));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await emails.SendNewTimeAnswerAsync(booking, accept, CancellationToken.None);
        await notifier.NotifyAsync(booking, toBusiness: true,
            accept
                ? $"{booking.CustomerName} har godtatt ny tid {OsloTime.Format(booking.StartUtc)}." +
                  (booking.Status == BookingStatus.Venter ? " Bekreft timen." : "")
                : $"{booking.CustomerName} kan ikke komme {OsloTime.Format(booking.StartUtc)}. Timen er avlyst.",
            BookingNotifier.PanelLink(booking), CancellationToken.None);
        return accept ? NewTimeOutcome.Accepted : NewTimeOutcome.Declined;
    }

    // "Møtte ikke" blocks the review, so a customer with an account is told and can object (e-mail + bell).
    // "Møtte" after "Møtte ikke" was a mistake: only that bell goes away again. Walk-ins cannot review: nothing to tell
    private async Task NotifyNoShowAsync(Booking booking, bool wasNoShow, CancellationToken cancellationToken)
    {
        if (booking.CustomerUserId is null)
        {
            return;
        }

        if (booking.Status == BookingStatus.MotteIkke)
        {
            // The e-mail only the first time (switching Møtte / Møtte ikke back and forth must not flood the customer)
            if (booking.NoShowEmailSentUtc is null)
            {
                booking.NoShowEmailSentUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                await emails.SendNoShowAsync(booking, CancellationToken.None);
            }

            await notifier.NotifyAsync(booking, toBusiness: false,
                $"{{0}} har registrert at du ikke møtte {OsloTime.Format(booking.StartUtc)}. Var du der? Se e-posten.",
                "/min-besti", CancellationToken.None);
        }
        else if (wasNoShow)
        {
            dbContext.Notifications.RemoveRange(await dbContext.Notifications
                .Where(n => n.UserId == booking.CustomerUserId && n.BookingId == booking.Id
                                                               && n.Text.Contains("ikke møtte"))
                .ToListAsync(cancellationToken));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    // An optional message when cancelling: trimmed, empty = none, at most 300 characters
    private static string? ShortNote(string? message) =>
        string.IsNullOrWhiteSpace(message) ? null : message.Trim()[..Math.Min(message.Trim().Length, 300)];

    // Validate the booking request
    private static string? Validate(CreateBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 100)
        {
            return "Navn er påkrevd (maks 100 tegn).";
        }

        if (!PhoneNumber.IsValid(request.CustomerPhone))
        {
            return "Oppgi et gyldig telefonnummer, f.eks. 912 34 567.";
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || request.CustomerEmail.Length > 250 ||
            !request.CustomerEmail.Contains('@'))
        {
            return "Oppgi en gyldig e-postadresse.";
        }

        if (request.Note is { Length: > 500 })
        {
            return "Meldingen kan være maks 500 tegn.";
        }

        return null;
    }

    private static string? ValidateManual(CreateManualBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 100)
        {
            return "Navn er påkrevd (maks 100 tegn).";
        }

        if (!PhoneNumber.IsValid(request.CustomerPhone))
        {
            return "Oppgi et gyldig telefonnummer, f.eks. 912 34 567.";
        }

        // E-mail is optional here (a walk-in customer may not give one)
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail)
            && (request.CustomerEmail.Length > 250 || !request.CustomerEmail.Contains('@')))
        {
            return "Oppgi en gyldig e-postadresse, eller la feltet stå tomt.";
        }

        if (request.Note is { Length: > 500 })
        {
            return "Notatet kan være maks 500 tegn.";
        }

        if (request.Date.Year is < 2000 or > 2100)
        {
            return "Ugyldig dato.";
        }

        return null;
    }

    // SQL Server lock per employee inside the open transaction; false when not granted within 10 s
    private async Task<bool> TryLockEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var lockName = $"booking-{employeeId}";
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

    // Local -> UTC; a time without a zone is taken as UTC (that is what the web app sends)
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };
}