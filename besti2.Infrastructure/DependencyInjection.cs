using besti2.Application.Accounts;
using besti2.Application.Bookings;
using besti2.Application.Businesses;
using besti2.Application.Email;
using besti2.Application.Geocoding;
using besti2.Application.Images;
using besti2.Application.Notifications;
using besti2.Application.Reviews;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using besti2.Infrastructure.Services.Bookings;
using besti2.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Address of the Next.js site, for the links in e-mails
        var webBaseUrl = configuration["WebBaseUrl"] ?? "http://localhost:3000";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<IBusinessSearchService, BusinessSearchService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IBookingService>(provider => new BookingService(
            provider.GetRequiredService<AppDbContext>(),
            provider.GetRequiredService<IEmailService>(),
            provider.GetRequiredService<ILogger<BookingService>>(),
            webBaseUrl));
        // Every e-mail goes through a queue and is sent in the background (see EmailQueue);
        // SmtpEmailService talks to the mail server itself
        services.AddTransient<SmtpEmailService>();
        services.AddSingleton<EmailQueue>();
        services.AddTransient<IEmailService, QueuedEmailService>();
        // One shared counter for the whole API (see MailLimiter)
        services.AddSingleton(new MailLimiter());
        services.AddScoped<IBusinessRegistrationService>(provider => new BusinessRegistrationService(
            provider.GetRequiredService<AppDbContext>(),
            provider.GetRequiredService<UserManager<AppUser>>(),
            provider.GetRequiredService<IGeocodingService>(),
            provider.GetRequiredService<IEmailService>(),
            webBaseUrl,
            provider.GetRequiredService<IBusinessRegistry>(),
            configuration["AdminEmail"],
            provider.GetRequiredService<MailLimiter>(),
            provider.GetRequiredService<IImageStorage>()));
        services.AddScoped<ICustomerAccountService>(provider => new CustomerAccountService(
            provider.GetRequiredService<UserManager<AppUser>>(),
            provider.GetRequiredService<IEmailService>(),
            webBaseUrl,
            provider.GetRequiredService<MailLimiter>()));
        services.AddHttpClient<IBusinessRegistry, BrregBusinessRegistry>(client =>
        {
            client.BaseAddress = new Uri("https://data.brreg.no/enhetsregisteret/api/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddHttpClient<IGeocodingService, GeonorgeGeocodingService>(client =>
        {
            client.BaseAddress = new Uri("https://ws.geonorge.no/adresser/v1/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<IImageStorage, LocalImageStorage>();
        services.AddScoped<IBusinessImageService, BusinessImageService>();
        services.AddScoped<IBookingAnonymizer, BookingAnonymizer>();
        // Reminders straight to the mail server, not the queue: a reminder that fails is counted and tried again
        services.AddScoped<IBookingReminder>(provider => new BookingReminder(
            provider.GetRequiredService<AppDbContext>(),
            provider.GetRequiredService<SmtpEmailService>(),
            provider.GetRequiredService<ILogger<BookingReminder>>(),
            webBaseUrl));
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ITimeOffService, TimeOffService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IProfileService, ProfileService>();
        // "Fortsett med Google": the client id from Google Cloud (empty = off)
        services.AddSingleton<IGoogleTokenValidator>(new GoogleTokenValidator(configuration["Google:ClientId"]));
        services.AddScoped<IGoogleLoginService, GoogleLoginService>();
        return services;
    }
}