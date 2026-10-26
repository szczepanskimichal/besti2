using besti2.Application.Accounts;
using besti2.Application.Bookings;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class ProfileService(AppDbContext dbContext, UserManager<AppUser> userManager, IBookingService bookingService)
    : IProfileService
{
    // Shown on reviews of a customer who deleted the account
    public const string FormerCustomer = "Tidligere kunde";
    public const string DeletedCustomer = "Slettet kunde";

    public async Task<ProfileDto?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null || user.BusinessId is not null
            ? null
            : new ProfileDto(user.FullName ?? "", user.Email ?? "", user.PhoneNumber);
    }
    public async Task<MyDataExport?> ExportAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await CustomerAsync(userId);
        if (user is null)
        {
            return null;
        }

        var bookings = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.CustomerUserId == userId)
            .OrderBy(b => b.StartUtc)
            .Select(b => new ExportBooking(b.StartUtc, b.EndUtc, b.Status.ToString(), b.Service.Business.Name,
                b.Service.Name, b.Employee.Name + " " + b.Employee.LastName, b.CustomerName, b.CustomerPhone,
                b.CustomerEmail, b.CustomerNote, b.MarketingConsent, b.MarketingConsentUtc))
            .ToListAsync(cancellationToken);
        var reviews = await dbContext.Reviews.AsNoTracking()
            .Where(r => r.CustomerUserId == userId)
            .OrderBy(r => r.CreatedUtc)
            .Select(r => new ExportReview(r.CreatedUtc,
                dbContext.Businesses.Where(x => x.Id == r.BusinessId).Select(x => x.Name).First(),
                r.Stars, r.Comment, r.BusinessReply))
            .ToListAsync(cancellationToken);
        var notifications = await dbContext.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.CreatedUtc)
            .Select(n => new ExportNotification(n.CreatedUtc, n.Text, n.ReadUtc != null))
            .ToListAsync(cancellationToken);

        return new MyDataExport(DateTime.UtcNow,
            new ExportAccount(user.FullName ?? "", user.Email ?? "", user.PhoneNumber, user.EmailConfirmed),
            bookings, reviews, notifications);
    }

    public async Task<string?> UpdateAsync(string userId, UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await CustomerAsync(userId);
        if (user is null)
        {
            return "Fant ikke kontoen.";
        }

        var name = request.FullName?.Trim() ?? "";
        if (name.Length is 0 or > 100)
        {
            return "Navn er påkrevd (maks 100 tegn).";
        }
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        if (phone is not null && !IsValidPhone(phone))
        {
            return "Oppgi et gyldig telefonnummer, f.eks. 912 34 567, eller la feltet stå tomt.";
        }

        user.FullName = name;
        user.PhoneNumber = phone;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? null : "Kunne ikke lagre. Prøv igjen.";
    }

    public async Task<string?> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await CustomerAsync(userId);
        if (user is null)
        {
            return "Fant ikke kontoen.";
        }
        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return "Det nye passordet må ha minst 6 tegn.";
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword ?? "", request.NewPassword);
        if (result.Succeeded)
        {
            return null;
        }
        return result.Errors.Any(e => e.Code == "PasswordMismatch")
            ? "Nåværende passord er feil."
            : "Passordet kunne ikke endres. Prøv et annet passord.";
    }

    public async Task<string?> DeleteAsync(string userId, DeleteAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await CustomerAsync(userId);
        if (user is null)
        {
            return "Fant ikke kontoen.";
        }
        if (!await userManager.CheckPasswordAsync(user, request.Password ?? ""))
        {
            return "Passordet er feil.";
        }

        // 1. Coming visits are cancelled the normal way: the business gets the e-mail and the bell
        var now = DateTime.UtcNow;
        var coming = await dbContext.Bookings
            .Where(b => b.CustomerUserId == userId && b.Status != BookingStatus.Avlyst && b.StartUtc > now)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
        foreach (var bookingId in coming)
        {
            await bookingService.CancelByCustomerAsync(bookingId, userId, cancellationToken: cancellationToken);        }

        // 2. All visits stay for the business's history, but without who it was
        var bookings = await dbContext.Bookings.Where(b => b.CustomerUserId == userId).ToListAsync(cancellationToken);
        foreach (var booking in bookings)
        {
            booking.CustomerName = DeletedCustomer;
            booking.CustomerPhone = "-";
            booking.CustomerEmail = BookingAnonymizer.AnonymizedEmail;
            booking.CustomerNote = null;
            booking.MarketingConsent = false;
            booking.MarketingConsentUtc = null;
            booking.NewTimeTokenHash = null;
            booking.CustomerUserId = null;
        }

        // 3. Reviews keep their stars and text (the business's rating stays fair), but no longer point to a person
        var reviews = await dbContext.Reviews.Where(r => r.CustomerUserId == userId).ToListAsync(cancellationToken);
        foreach (var review in reviews)
        {
            review.CustomerName = FormerCustomer;
            review.CustomerUserId = "slettet";
        }

        // 4. The customer's bell
        dbContext.Notifications.RemoveRange(
            await dbContext.Notifications.Where(n => n.UserId == userId).ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. The account itself
        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? null : "Kontoen kunne ikke slettes. Prøv igjen.";
    }

    // Only customer accounts; a business owner cannot use these (a business has its own data)
    private async Task<AppUser?> CustomerAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null || user.BusinessId is not null ? null : user;
    }

    // Digits, spaces and an optional + only, at least 8 digits (as for bookings)
    private static bool IsValidPhone(string phone) =>
        phone.Length <= 20
        && phone.All(c => char.IsDigit(c) || c == ' ' || c == '+')
        && phone.Count(char.IsDigit) >= 8
        && phone.LastIndexOf('+') <= 0;
}
