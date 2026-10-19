using System.Security.Claims;
using besti2.Application.Accounts;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Bookings;
using besti2.Infrastructure;
using besti2.Application.Businesses;
using besti2.Application.Images;
using besti2.Domain.Enums;
using besti2.Identity;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Jobs;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
// Add authentication and authorization
builder.Services.AddAuthorization();
// Login only after the email is confirmed (business owners are confirmed at registration)
builder.Services.AddIdentityApiEndpoints<AppUser>(options => options.SignIn.RequireConfirmedEmail = true)
    .AddEntityFrameworkStores<AppDbContext>()
    ;
builder.Services.AddHostedService<BookingAnonymizationJob>();
// Password reset mails from Identity go through our email service
builder.Services.AddTransient<IEmailSender<AppUser>, IdentityEmailSender>();

var app = builder.Build();
//--------------------------------------
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevUserSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
// Uploaded images (development storage), e.g. /uploads/abc.webp
var uploadsFolder = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsFolder);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsFolder),
    RequestPath = "/uploads"
});
app.UseAuthentication();
app.UseAuthorization();
// Get all businesses (optional filters: category, city, text q)
app.MapGet("/api/businesses",
    async (string? category, string? city, string? q, IBusinessService businessService,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(await businessService.GetAllAsync(category, city, q, cancellationToken));
    });
// Get distinct cities for the filter
app.MapGet("/api/cities",
    async (IBusinessService businessService, CancellationToken cancellationToken) =>
    {
        return Results.Ok(await businessService.GetCitiesAsync(cancellationToken));
    });
// Get business by id
app.MapGet("/api/businesses/{id:guid}",
    async (Guid id, IBusinessService businessService, CancellationToken cancellationToken) =>
    {
        var business = await businessService.GetByIdAsync(id, cancellationToken);
        return business is null ? Results.NotFound() : Results.Ok(business);
    });
// Get availability for a service and employee on a specific date
app.MapGet("/api/availability", async (Guid serviceId, Guid employeeId, DateOnly date, IBookingService bookingService,
    CancellationToken cancellationToken) =>
{
    var slots = await bookingService.GetFreeSlotsAsync(serviceId, employeeId, date, cancellationToken);
    return slots is null ? Results.NotFound() : Results.Ok(slots);
});

// Create a new booking (customers only; name and email come from the account)
app.MapPost("/api/bookings",
    async (CreateBookingRequest request, ClaimsPrincipal principal, UserManager<AppUser> userManager,
        IBookingService bookingService, CancellationToken cancellationToken) =>
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null || user.BusinessId is not null)
        {
            return Results.BadRequest(new { message = "Logg inn med en kundekonto for å bestille." });
        }
    // Never trust name/email from the form: use the account's
        var fromAccount = request with { CustomerName = user.FullName ?? "", CustomerEmail = user.Email ?? "" };
        var result = await bookingService.CreateAsync(fromAccount, user.Id, cancellationToken);
        return result.Outcome switch
        {
            CreateBookingOutcome.Created => Results.Created($"/api/bookings/{result.Booking!.Id}", result.Booking),
            CreateBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
            CreateBookingOutcome.SlotTaken => Results.Conflict(
                new { message = "Tiden er dessverre ikke lenger ledig." }),
            _ => Results.NotFound()
        };
    }).RequireAuthorization();

// Register a new business with its owner account
app.MapPost("/api/businesses/register", async (RegisterBusinessRequest request,
    IBusinessRegistrationService registrationService, CancellationToken cancellationToken) =>
{
    var result = await registrationService.RegisterAsync(request, cancellationToken);
    return result.Success
        ? Results.Created()
        : Results.BadRequest(new { message = result.Error });
});

// Customer sign-up: creates the account and sends a confirmation mail
app.MapPost("/api/account/register", async (RegisterCustomerRequest request,
    ICustomerAccountService accountService, CancellationToken cancellationToken) =>
{
    var error = await accountService.RegisterAsync(request, cancellationToken);
    return error is null ? Results.Created() : Results.BadRequest(new { message = error });
});

// Customer clicks the link in the mail; the web page sends the id and code here
app.MapPost("/api/account/confirm-email", async (ConfirmEmailRequest request,
    ICustomerAccountService accountService) =>
{
    var confirmed = await accountService.ConfirmEmailAsync(request);
    return confirmed
        ? Results.NoContent()
        : Results.BadRequest(new { message = "Lenken er ugyldig eller utløpt." });
});
// Who is logged in: the web uses isBusiness to choose between /panel and the customer pages
app.MapGet("/api/me", async (ClaimsPrincipal principal, UserManager<AppUser> userManager) =>
{
    var user = await userManager.GetUserAsync(principal);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new { user.Email, user.FullName, IsBusiness = user.BusinessId is not null });
}).RequireAuthorization();

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

// Business panel: the owner's own business with services and employees
app.MapGet("/api/panel/business", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
    IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var business = await businessService.GetByIdAsync(businessId, cancellationToken);
    return business is null ? Results.NotFound() : Results.Ok(business);
}).RequireAuthorization();

// Business panel: add a service to the owner's business
app.MapPost("/api/panel/services", async (CreateServiceRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var error = await businessService.AddServiceAsync(businessId, request, cancellationToken);
    return error is null ? Results.Created() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Business panel: edit one of the owner's services
app.MapPut("/api/panel/services/{id:guid}", async (Guid id, CreateServiceRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var error = await businessService.UpdateServiceAsync(businessId, id, request, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Business panel: soft delete one of the owner's services
app.MapDelete("/api/panel/services/{id:guid}", async (Guid id, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var deleted = await businessService.DeleteServiceAsync(businessId, id, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

// Business panel: add an employee
app.MapPost("/api/panel/employees", async (EmployeeRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);

    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var error = await businessService.AddEmployeeAsync(businessId, request, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Business panel: edit one of the owner's employees
app.MapPut("/api/panel/employees/{id:guid}", async (Guid id, EmployeeRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var error = await businessService.UpdateEmployeeAsync(businessId, id, request, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Business panel: soft delete one of the owner's employees (never the last one)
app.MapDelete("/api/panel/employees/{id:guid}", async (Guid id, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var error = await businessService.DeleteEmployeeAsync(businessId, id, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Business panel: owner edits description and contact info
app.MapPut("/api/panel/business", async (UpdateProfileRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var error = await businessService.UpdateProfileAsync(businessId, request, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Business panel: owner updates opening hours
app.MapPut("/api/panel/opening-hours", async (UpdateOpeningHoursRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var error = await businessService.UpdateOpeningHoursAsync(businessId, request, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Business panel: owner uploads a logo (PNG, JPG or WebP, max 10 MB, saved as small WebP)
app.MapPost("/api/panel/logo", async (IFormFile file, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessImageService imageService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    await using var stream = file.OpenReadStream();
    var error = await imageService.SetLogoAsync(businessId, stream, file.Length, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization().DisableAntiforgery();

// Identity endpoints: /api/auth/register, /api/auth/login, /api/auth/refresh ...
var auth = app.MapGroup("/api/auth");
// Block Identity's open /register; owners sign up via business registration instead
auth.AddEndpointFilter(async (context, next) =>
    context.HttpContext.Request.Path.Value!.EndsWith("/register", StringComparison.OrdinalIgnoreCase)
        ? Results.NotFound()
        : await next(context));
auth.MapIdentityApi<AppUser>();

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
        ChangeBookingStatusAsync(id, BookingStatus.Bekreftet, principal, userManager, bookingService,
            cancellationToken))
    .RequireAuthorization();

// Cancel a booking
app.MapPost("/api/panel/bookings/{id:guid}/avlys", (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
        ChangeBookingStatusAsync(id, BookingStatus.Avlyst, principal, userManager, bookingService, cancellationToken))
    .RequireAuthorization();


app.Run();