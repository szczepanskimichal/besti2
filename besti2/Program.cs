using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Bookings;
using besti2.Infrastructure;
using besti2.Application.Businesses;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
// Add authentication and authorization
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<AppDbContext>();
var app = builder.Build();
//--------------------------------------
// Configure the HTTP request pipeline.
 if (app.Environment.IsDevelopment())
 {
     app.MapOpenApi();
     await DevUserSeeder.SeedAsync(app.Services);
 }

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
// Get all businesses
app.MapGet("/api/businesses", async (IBusinessService businessService, CancellationToken cancellationToken) =>
{
   return Results.Ok(await businessService.GetAllAsync(cancellationToken));
});
// Get business by id
app.MapGet("/api/businesses/{id:guid}", async (Guid id, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var business = await businessService.GetByIdAsync(id, cancellationToken);
    return business is null ? Results.NotFound() : Results.Ok(business);
});
// Get availability for a service and employee on a specific date
app.MapGet("/api/availability", async (Guid serviceId, Guid employeeId, DateOnly date, IBookingService bookingService, CancellationToken cancellationToken) =>
{
    var slots = await bookingService.GetFreeSlotsAsync(serviceId, employeeId, date, cancellationToken);
    return slots is null ? Results.NotFound() : Results.Ok(slots);
});
// Create a new booking
app.MapPost("/api/bookings", async (CreateBookingRequest request, IBookingService bookingService, CancellationToken cancellationToken) =>
{
    var result = await bookingService.CreateAsync(request, cancellationToken);

    return result.Outcome switch
    {
        CreateBookingOutcome.Created => Results.Created($"/api/bookings/{result.Booking!.Id}", result.Booking),
        CreateBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
        CreateBookingOutcome.SlotTaken => Results.Conflict(new { message = "Tiden er dessverre ikke lenger ledig." }),
        _ => Results.NotFound()
    };
});
// Business panel: bookings for the logged-in owner's business
app.MapGet("/api/panel/bookings", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
    IBookingService bookingService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    return Results.Ok(await bookingService.GetForBusinessAsync(businessId, cancellationToken));
}).RequireAuthorization();

// Identity endpoints: /api/auth/register, /api/auth/login, /api/auth/refresh ...
app.MapGroup("/api/auth").MapIdentityApi<AppUser>();

// Shared logic for panel status changes (confirm / cancel)
async Task<IResult> ChangeBookingStatusAsync(Guid id, BookingStatus newStatus, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken)
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var outcome = await bookingService.UpdateStatusAsync(id, businessId, newStatus, cancellationToken);

    return outcome switch
    {
        UpdateBookingStatusOutcome.Updated => Results.NoContent(),
        UpdateBookingStatusOutcome.InvalidTransition =>
            Results.Conflict(new { message = "Bestillingen kan ikke endres til denne statusen." }),
        _ => Results.NotFound()
    };
}


// Confirm a booking
app.MapPost("/api/panel/bookings/{id:guid}/bekreft", (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
        ChangeBookingStatusAsync(id, BookingStatus.Bekreftet, principal, userManager, bookingService, cancellationToken))
    .RequireAuthorization();

// Cancel a booking
app.MapPost("/api/panel/bookings/{id:guid}/avlys", (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
        ChangeBookingStatusAsync(id, BookingStatus.Avlyst, principal, userManager, bookingService, cancellationToken))
    .RequireAuthorization();

app.Run();