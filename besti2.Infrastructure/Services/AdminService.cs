using besti2.Application.Admin;
using besti2.Application.Bookings;
using besti2.Application.Common;
using besti2.Application.Email;
using besti2.Application.Images;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// emailService: the owner's e-mail when BESTI closes the business page; storage: photos on reviews
public class AdminService(AppDbContext dbContext, IEmailService? emailService = null, IImageStorage? storage = null)
    : IAdminService
{
    public const int NoteMaxLength = 2000;

    // The only reasons to hide a review (the customer sees the reason)
    public static readonly string[] ReviewReasons =
    [
        "Hets, trusler eller grovt språk",
        "Personopplysninger om andre",
        "Ikke en ekte vurdering av besøket",
        "Reklame eller spam",
        PhotoReason
    ];

    // Hiding with this reason also deletes the photo (it cannot come back with "Vis igjen")
    public const string PhotoReason = "Bildet passer ikke";

    // The only reasons to close a business page (the owner sees the reason)
    public static readonly string[] SuspendReasons =
    [
        "Vi kan ikke bekrefte at bedriften er ekte",
        "Flere klager fra kunder",
        "Bedriften er ikke lenger i drift",
        "Brudd på vilkårene for BESTI",
        "Eieren har bedt om det"
    ];

    // Weeks in "Utvikling" on the overview
    public const int GrowthWeeks = 8;

    public async Task<AdminOverviewDto> GetOverviewAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var rows = await RowsAsync(dbContext.Businesses, nowUtc, cancellationToken);
        var monthAgo = nowUtc.AddDays(-30);
        return new AdminOverviewDto(
            VisibleBusinesses: rows.Count(r => r.Status == "Synlig"),
            WaitingForApproval: rows.Count(r => r.Status == "Venter"),
            UnconfirmedSignups: rows.Count(r => r.Status == "Ubekreftet"),
            CustomerAccounts: await dbContext.Users.CountAsync(u => u.BusinessId == null, cancellationToken),
            BookingsLast7Days: await dbContext.Bookings.CountAsync(b => b.CreatedUtc >= nowUtc.AddDays(-7),
                cancellationToken),
            VisitsLast30Days: await dbContext.Bookings.CountAsync(b => b.Status == BookingStatus.Bekreftet
                                                                       && b.StartUtc >= monthAgo
                                                                       && b.EndUtc <= nowUtc, cancellationToken),
            ReviewsLast30Days: await dbContext.Reviews.CountAsync(r => r.CreatedUtc >= monthAgo, cancellationToken),
            ProInterested: rows.Count(r => r.ProInterest),
            Waiting: rows.Where(r => r.Status == "Venter").OrderBy(r => r.CreatedUtc).ToList(),
            // Visible, but customers cannot really book yet, or nobody has booked in a month: worth a call
            NeedHelp: rows.Where(r => r.Status == "Synlig"
                                      && (r.SetupDone < r.SetupTotal - 1 || r.BookingsLast30Days == 0))
                .OrderBy(r => r.SetupDone)
                .ThenBy(r => r.CreatedUtc)
                .Take(10)
                .ToList(),
            Weeks: await WeeksAsync(nowUtc, cancellationToken),
            // "Jeg er interessert" in Pro: the first ones to ask when Pro starts
            ProLeads: rows.Where(r => r.ProInterest && r.Status != "Stengt").OrderBy(r => r.Name).ToList());
    }

    private async Task<List<AdminWeekDto>> WeeksAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SlotCalculator.Oslo));
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var firstMonday = monday.AddDays(-7 * (GrowthWeeks - 1));
        var fromUtc = SlotCalculator.ToUtc(firstMonday, TimeOnly.MinValue);

        var businesses = await dbContext.Businesses.Where(b => b.CreatedUtc >= fromUtc)
            .Select(b => b.CreatedUtc).ToListAsync(cancellationToken);
        var customers = await dbContext.Users.Where(u => u.BusinessId == null && u.CreatedUtc >= fromUtc)
            .Select(u => u.CreatedUtc!.Value).ToListAsync(cancellationToken);
        var bookings = await dbContext.Bookings.Where(b => b.CreatedUtc >= fromUtc)
            .Select(b => b.CreatedUtc).ToListAsync(cancellationToken);

        return Enumerable.Range(0, GrowthWeeks).Select(i =>
        {
            var start = firstMonday.AddDays(7 * i);
            var startUtc = SlotCalculator.ToUtc(start, TimeOnly.MinValue);
            var endUtc = SlotCalculator.ToUtc(start.AddDays(7), TimeOnly.MinValue);
            bool InWeek(DateTime utc) => utc >= startUtc && utc < endUtc;
            return new AdminWeekDto(start, businesses.Count(InWeek), customers.Count(InWeek), bookings.Count(InWeek));
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminBusinessRow>> GetBusinessesAsync(string? status, string? q, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Businesses.AsQueryable();
        var search = q?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var ownerIds = dbContext.Users.Where(u => u.BusinessId != null && u.Email!.Contains(search))
                .Select(u => u.BusinessId);
            query = query.Where(b => b.Name.Contains(search) || b.City.Contains(search) || b.OrgNumber == search
                                     || ownerIds.Contains(b.Id));
        }

        var rows = await RowsAsync(query, nowUtc, cancellationToken);
        return rows.Where(r => string.IsNullOrEmpty(status) || r.Status == status)
            .OrderBy(r => r.Status == "Venter" ? 0 : 1)
            .ThenByDescending(r => r.CreatedUtc)
            .ToList();
    }

    public async Task<AdminBusinessDetailsDto?> GetBusinessAsync(Guid id, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var row = (await RowsAsync(dbContext.Businesses.Where(b => b.Id == id), nowUtc, cancellationToken)).Single();
        var setup = await SetupAsync(dbContext.Businesses.Where(b => b.Id == id), cancellationToken);
        return new AdminBusinessDetailsDto(
            row,
            business.OrgNumber,
            business.Address,
            business.PostalCode,
            business.Email,
            business.Website,
            business.ReferralSource,
            business.ApprovedUtc,
            business.AdminNote,
            WeeklyReportOn: !business.WeeklyReportOff,
            ReviewCount: await dbContext.Reviews.CountAsync(r => r.BusinessId == id, cancellationToken),
            LastBookingCreatedUtc: await dbContext.Bookings.Where(b => b.Service.BusinessId == id)
                .MaxAsync(b => (DateTime?)b.CreatedUtc, cancellationToken),
            Setup: Steps(setup[id]),
            SuspendedUtc: business.SuspendedUtc,
            SuspendedReason: business.SuspendedReason,
            Log: await dbContext.AdminActions.AsNoTracking()
                .Where(a => a.BusinessId == id)
                .OrderByDescending(a => a.CreatedUtc)
                .Take(20)
                .Select(a => new AdminActionDto(a.CreatedUtc, a.AdminEmail, a.Text, a.BusinessId))
                .ToListAsync(cancellationToken));
    }

    public async Task<string?> SetNoteAsync(Guid id, string? note, CancellationToken cancellationToken = default)
    {
        var text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (text?.Length > NoteMaxLength)
        {
            return $"Notatet kan være maks {NoteMaxLength} tegn.";
        }

        var business = await dbContext.Businesses.FindAsync([id], cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        business.AdminNote = text;
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<AdminReviewDto>> GetLatestReviewsAsync(int count, bool onlyHidden = false,
        CancellationToken cancellationToken = default)
    {
        var rows = await (from review in dbContext.Reviews.AsNoTracking()
                join business in dbContext.Businesses on review.BusinessId equals business.Id
                where !onlyHidden || review.HiddenUtc != null
                orderby review.CreatedUtc descending
                select new
                {
                    review.Id, BusinessName = business.Name, business.Slug, BusinessId = business.Id,
                    review.CustomerName, review.Stars, review.Comment, review.BusinessReply, review.CreatedUtc,
                    review.PhotoFileName, review.HiddenUtc, review.HiddenReason
                })
            .Take(Math.Clamp(count, 1, 200))
            .ToListAsync(cancellationToken);
        return rows.Select(r => new AdminReviewDto(r.Id, r.BusinessName, r.Slug, r.BusinessId, r.CustomerName,
                r.Stars, r.Comment, r.BusinessReply, r.CreatedUtc,
                r.PhotoFileName is null ? null : storage?.GetUrl(r.PhotoFileName), r.HiddenUtc, r.HiddenReason))
            .ToList();
    }

    public async Task<string?> HideReviewAsync(Guid reviewId, string? reason, string adminEmail,
        CancellationToken cancellationToken = default)
    {
        if (reason is null || !ReviewReasons.Contains(reason))
        {
            return "Velg en grunn.";
        }

        var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return "Fant ikke vurderingen.";
        }

        if (review.HiddenUtc is not null)
        {
            return "Vurderingen er allerede skjult.";
        }

        var businessName = await BusinessNameAsync(review.BusinessId, cancellationToken);
        review.HiddenUtc = DateTime.UtcNow;
        review.HiddenReason = reason;
        // A photo that does not belong there is deleted for good: the file address could have been shared already
        var photo = reason == PhotoReason ? review.PhotoFileName : null;
        if (photo is not null)
        {
            review.PhotoFileName = null;
        }
        // The customer learns why (a deleted or anonymized account has no bell)
        if (review.CustomerUserId != BookingAnonymizer.AnonymizedUserId)
        {
            Notify(review.CustomerUserId, null,
                $"BESTI har skjult vurderingen din av {businessName}: {reason}. Spørsmål? Skriv til post@bestiapp.no",
                "/min-besti");
        }

        Notify(null, review.BusinessId, $"BESTI har skjult en vurdering ({reason}). Den teller ikke i stjernene.",
            "/panel/vurderinger");
        AddLog(adminEmail, $"Skjulte vurdering ({review.Stars} ★) av {businessName}: {reason}", review.BusinessId);
        await dbContext.SaveChangesAsync(cancellationToken);
        // The file only after the database no longer points to it
        if (photo is not null && storage is not null)
        {
            await storage.DeleteAsync(photo, cancellationToken);
        }

        return null;
    }

    public async Task<string?> ShowReviewAsync(Guid reviewId, string adminEmail,
        CancellationToken cancellationToken = default)
    {
        var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return "Fant ikke vurderingen.";
        }

        if (review.HiddenUtc is null)
        {
            return "Vurderingen er ikke skjult.";
        }

        review.HiddenUtc = null;
        review.HiddenReason = null;
        AddLog(adminEmail,
            $"Viste vurdering ({review.Stars} ★) av {await BusinessNameAsync(review.BusinessId, cancellationToken)} igjen",
            review.BusinessId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> SuspendBusinessAsync(Guid id, string? reason, string adminEmail,
        CancellationToken cancellationToken = default)
    {
        if (reason is null || !SuspendReasons.Contains(reason))
        {
            return "Velg en grunn.";
        }

        var business = await dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        if (business.SuspendedUtc is not null)
        {
            return "Bedriften er allerede stengt.";
        }

        // Only a business customers can see; one waiting for approval is rejected instead
        if (business.IsHidden)
        {
            return "Bare synlige bedrifter kan stenges. En bedrift som venter, avvises i stedet.";
        }

        business.IsHidden = true;
        business.SuspendedUtc = DateTime.UtcNow;
        business.SuspendedReason = reason;
        var upcoming = await dbContext.Bookings.CountAsync(b => b.Service.BusinessId == id
                                                                && b.StartUtc > DateTime.UtcNow
                                                                && b.Status != BookingStatus.Avlyst
                                                                && b.Status != BookingStatus.MotteIkke,
            cancellationToken);
        Notify(null, id, $"BESTI har stengt bedriftssiden: {reason}. Kommende bestillinger gjelder fortsatt.",
            "/panel");
        AddLog(adminEmail, $"Stengte {business.Name}: {reason} ({upcoming} kommende bestillinger beholdt)", id);
        await dbContext.SaveChangesAsync(cancellationToken);

        await MailOwnerAsync(id,
            "Bedriftssiden din på BESTI er stengt",
            "Hei!\n\n" +
            $"BESTI har stengt siden til {business.Name}, så kundene ikke finner den og ikke kan bestille nye timer.\n\n" +
            $"Grunn: {reason}\n\n" +
            "Bestillinger som allerede er gjort, gjelder fortsatt, og du kan bruke panelet som før.\n" +
            "Mener du at dette er feil? Svar på denne e-posten eller skriv til post@bestiapp.no, så ser vi på det.\n\n" +
            "Hilsen BESTI", cancellationToken);
        return null;
    }

    public async Task<string?> ReopenBusinessAsync(Guid id, string adminEmail,
        CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        if (business.SuspendedUtc is null)
        {
            return "Bedriften er ikke stengt.";
        }

        business.IsHidden = false;
        business.SuspendedUtc = null;
        business.SuspendedReason = null;
        Notify(null, id, "Bedriftssiden er åpen igjen. Kundene kan bestille som før.", "/panel");
        AddLog(adminEmail, $"Åpnet {business.Name} igjen", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await MailOwnerAsync(id, "Bedriftssiden din på BESTI er åpen igjen",
            $"Hei!\n\nSiden til {business.Name} er synlig igjen, og kundene kan bestille som før.\n\nHilsen BESTI",
            cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<AdminCustomerDto>> FindCustomersAsync(string query, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var text = query?.Trim() ?? "";
        if (text.Length < 5)
        {
            return [];
        }

        List<AppUserRow> users;
        if (text.Contains('@'))
        {
            var normalized = text.ToUpperInvariant();
            users = await UsersAsync(dbContext.Users.Where(u => u.NormalizedEmail == normalized), cancellationToken);
        }
        else
        {
            // "912 34 567", "+47 912 34 567" and "91234567" are the same number
            var digits = new string(text.Where(char.IsDigit).ToArray());
            if (digits.Length == 10 && digits.StartsWith("47"))
            {
                digits = digits[2..];
            }

            if (digits.Length != 8)
            {
                return [];
            }

            var variants = new[] { digits, "+47" + digits, $"{digits[..3]} {digits[3..5]} {digits[5..]}" };
            users = await UsersAsync(dbContext.Users.Where(u => u.PhoneNumber != null
                                                                && variants.Contains(u.PhoneNumber.Replace(" ", ""))),
                cancellationToken);
        }

        var result = new List<AdminCustomerDto>();
        foreach (var user in users.Take(10))
        {
            var upcoming = await dbContext.Bookings.AsNoTracking()
                .Where(b => b.CustomerUserId == user.Id && b.StartUtc > nowUtc)
                .OrderBy(b => b.StartUtc)
                .Take(10)
                .Select(b => new AdminCustomerBookingDto(b.Id, b.Service.Business.Name, b.Service.Name, b.StartUtc,
                    b.Status.ToString()))
                .ToListAsync(cancellationToken);
            var mails = await MailsAsync(dbContext.OutboxEmails.Where(m => m.To == user.Email), cancellationToken);
            result.Add(new AdminCustomerDto(
                user.Id, user.Email ?? "", user.FullName, user.PhoneNumber, user.EmailConfirmed,
                HasPassword: user.PasswordHash != null,
                HasGoogle: await dbContext.UserLogins.AnyAsync(l => l.UserId == user.Id, cancellationToken),
                user.CreatedUtc,
                Visits: await dbContext.Bookings.CountAsync(b => b.CustomerUserId == user.Id
                                                                 && b.Status == BookingStatus.Bekreftet
                                                                 && b.EndUtc <= nowUtc, cancellationToken),
                Reviews: await dbContext.Reviews.CountAsync(r => r.CustomerUserId == user.Id, cancellationToken),
                upcoming,
                mails,
                StaffAt: await dbContext.Employees.Where(e => e.StaffUserId == user.Id && !e.IsDeleted)
                    .Select(e => e.Business.Name)
                    .ToListAsync(cancellationToken)));
        }

        return result;
    }

    public async Task<AdminHealthDto> GetHealthAsync(string? version, DateTime startedUtc, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var databaseOk = await dbContext.Database.CanConnectAsync(cancellationToken);
        double? sizeMb = null;
        DateTime? lastBackupUtc = null;
        if (databaseOk && dbContext.Database.IsRelational())
        {
            try
            {
                // The data and log files of this database (Express allows 10 GB of data)
                sizeMb = await dbContext.Database
                    .SqlQueryRaw<double>("SELECT CAST(SUM(CAST(size AS bigint)) * 8.0 / 1024 AS float) AS [Value] FROM sys.database_files")
                    .SingleAsync(cancellationToken);
                // The newest full backup SQL Server made of this database (backup.sh every night)
                lastBackupUtc = await dbContext.Database
                    .SqlQueryRaw<DateTime?>("SELECT MAX(backup_finish_date) AS [Value] FROM msdb.dbo.backupset WHERE database_name = DB_NAME() AND type = 'D'")
                    .SingleAsync(cancellationToken);
                if (lastBackupUtc is DateTime local)
                {
                    // msdb keeps the server's local time (Oslo on bestiapp.no)
                    lastBackupUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
                        TimeZoneInfo.Local);
                }
            }
            catch (Exception)
            {
                // The API's database login may not see these; the page shows "ukjent" then
            }
        }

        var waiting = dbContext.OutboxEmails.AsNoTracking();
        return new AdminHealthDto(
            version,
            startedUtc,
            databaseOk,
            sizeMb is double mb ? Math.Round(mb, 1) : null,
            lastBackupUtc,
            MailsWaiting: await waiting.CountAsync(cancellationToken),
            MailsFailing: await waiting.CountAsync(m => m.Attempts > 0, cancellationToken),
            FailingMails: await MailsAsync(waiting.Where(m => m.Attempts > 0), cancellationToken),
            PushDevices: await dbContext.WebPushSubscriptions.CountAsync(cancellationToken),
            Jobs: JobHeartbeats.All().Select(j => new AdminJobDto(j.Job, j.LastRunUtc, j.Ok)).ToList());
    }

    public async Task<IReadOnlyList<AdminActionDto>> GetLogAsync(int count,
        CancellationToken cancellationToken = default) =>
        await dbContext.AdminActions.AsNoTracking()
            .OrderByDescending(a => a.CreatedUtc)
            .Take(Math.Clamp(count, 1, 500))
            .Select(a => new AdminActionDto(a.CreatedUtc, a.AdminEmail, a.Text, a.BusinessId))
            .ToListAsync(cancellationToken);

    public async Task LogAsync(string adminEmail, string text, Guid? businessId,
        CancellationToken cancellationToken = default)
    {
        AddLog(adminEmail, text, businessId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddLog(string adminEmail, string text, Guid? businessId) =>
        dbContext.AdminActions.Add(new AdminAction
        {
            Id = Guid.NewGuid(),
            AdminEmail = adminEmail,
            Text = text.Length > 500 ? text[..500] : text,
            BusinessId = businessId,
            CreatedUtc = DateTime.UtcNow
        });

    private void Notify(string? userId, Guid? businessId, string text, string link) =>
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessId = businessId,
            Text = text.Length > 300 ? text[..300] : text,
            Link = link,
            CreatedUtc = DateTime.UtcNow
        });

    private async Task<string> BusinessNameAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Businesses.Where(b => b.Id == id).Select(b => b.Name).FirstOrDefaultAsync(cancellationToken)
        ?? "bedriften";

    private async Task MailOwnerAsync(Guid businessId, string subject, string body,
        CancellationToken cancellationToken)
    {
        if (emailService is null)
        {
            return;
        }

        var owner = await dbContext.Users
            .Where(u => u.BusinessId == businessId && u.EmailConfirmed)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (owner is not null)
        {
            await emailService.SendAsync(owner, subject, body, cancellationToken);
        }
    }

    private static Task<List<AppUserRow>> UsersAsync(IQueryable<Identity.AppUser> users,
        CancellationToken cancellationToken) =>
        // Customer accounts only: a business owner is found under Bedrifter
        users.Where(u => u.BusinessId == null)
            .Select(u => new AppUserRow(u.Id, u.Email, u.FullName, u.PhoneNumber, u.EmailConfirmed, u.PasswordHash,
                u.CreatedUtc))
            .ToListAsync(cancellationToken);

    private static Task<List<AdminMailDto>> MailsAsync(IQueryable<OutboxEmail> mails,
        CancellationToken cancellationToken) =>
        mails.OrderBy(m => m.CreatedUtc)
            .Take(20)
            .Select(m => new AdminMailDto(m.To, m.Subject, m.CreatedUtc, m.Attempts, m.NextAttemptUtc))
            .ToListAsync(cancellationToken);

    private sealed record AppUserRow(
        string Id,
        string? Email,
        string? FullName,
        string? PhoneNumber,
        bool EmailConfirmed,
        string? PasswordHash,
        DateTime? CreatedUtc);

    private async Task<List<AdminBusinessRow>> RowsAsync(IQueryable<Business> businesses, DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var list = await businesses.AsNoTracking()
            .Select(b => new
            {
                b.Id, b.Name, b.Slug, b.City, b.Category, b.IsHidden, b.ApprovedUtc, b.ApprovalTokenHash, b.CreatedUtc,
                b.PhoneNumber, b.VerifiedUtc, b.ProInterestUtc, b.AdminNote, b.SuspendedUtc
            })
            .ToListAsync(cancellationToken);
        var ids = list.Select(b => b.Id).ToList();
        var monthAgo = nowUtc.AddDays(-30);
        var owners = await dbContext.Users.Where(u => u.BusinessId != null && ids.Contains(u.BusinessId.Value))
            .Select(u => new { u.BusinessId, u.Email })
            .ToListAsync(cancellationToken);
        var bookings = await dbContext.Bookings.Where(b => ids.Contains(b.Service.BusinessId) && b.CreatedUtc >= monthAgo)
            .GroupBy(b => b.Service.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var upcoming = await dbContext.Bookings.Where(b => ids.Contains(b.Service.BusinessId) && b.StartUtc >= nowUtc
                                                           && b.Status != BookingStatus.Avlyst)
            .GroupBy(b => b.Service.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var stars = await dbContext.Reviews.Where(r => ids.Contains(r.BusinessId) && r.HiddenUtc == null)
            .GroupBy(r => r.BusinessId)
            .Select(g => new { g.Key, Average = g.Average(r => r.Stars) })
            .ToDictionaryAsync(g => g.Key, g => g.Average, cancellationToken);
        var setup = await SetupAsync(businesses, cancellationToken);

        return list.Select(b =>
        {
            var steps = Steps(setup[b.Id]);
            return new AdminBusinessRow(
                b.Id, b.Name, b.Slug, b.City, b.Category.ToString(),
                Status: b.SuspendedUtc != null ? "Stengt"
                    : !b.IsHidden ? "Synlig"
                    : b.ApprovalTokenHash != null ? "Venter"
                    : b.ApprovedUtc == null ? "Ubekreftet"
                    : "Skjult",
                b.CreatedUtc,
                owners.FirstOrDefault(o => o.BusinessId == b.Id)?.Email,
                b.PhoneNumber,
                Verified: b.VerifiedUtc != null,
                SetupDone: steps.Count(s => s.Done),
                SetupTotal: steps.Count,
                BookingsLast30Days: bookings.GetValueOrDefault(b.Id),
                Upcoming: upcoming.GetValueOrDefault(b.Id),
                AverageStars: stars.TryGetValue(b.Id, out var average) ? Math.Round(average, 1) : null,
                ProInterest: b.ProInterestUtc != null,
                HasNote: !string.IsNullOrEmpty(b.AdminNote));
        }).ToList();
    }

    private async Task<Dictionary<Guid, SetupCounts>> SetupAsync(IQueryable<Business> businesses,
        CancellationToken cancellationToken) =>
        await businesses.AsNoTracking()
            .Select(b => new SetupCounts(
                b.Id,
                b.Services.Count(s => !s.IsDeleted),
                b.OpeningHours.Count,
                b.Images.Any(i => i.Kind == ImageKind.Logo),
                b.Description != null && b.Description != "",
                b.Images.Count(i => i.Kind == ImageKind.Galleri),
                dbContext.Bookings.Any(x => x.Service.BusinessId == b.Id)))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

    private static List<AdminSetupStep> Steps(SetupCounts counts) =>
    [
        new("Tjenester", counts.Services > 0),
        new("Åpningstider", counts.OpeningHours > 0),
        new("Logo", counts.HasLogo),
        new("Beskrivelse", counts.HasDescription),
        new("Bilder", counts.Gallery > 0),
        new("Første bestilling", counts.HasBooking)
    ];

    private sealed record SetupCounts(
        Guid Id,
        int Services,
        int OpeningHours,
        bool HasLogo,
        bool HasDescription,
        int Gallery,
        bool HasBooking);
}
