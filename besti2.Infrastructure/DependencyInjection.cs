using besti2.Application.Bookings;
using besti2.Application.Businesses;
using besti2.Application.Email;
using besti2.Application.Geocoding;
using besti2.Application.Images;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IBusinessRegistrationService, BusinessRegistrationService>();
        services.AddHttpClient<IGeocodingService, GeonorgeGeocodingService>(client =>
        {
            client.BaseAddress = new Uri("https://ws.geonorge.no/adresser/v1/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<IImageStorage, LocalImageStorage>();
        return services;
    }

}