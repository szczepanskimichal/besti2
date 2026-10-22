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
using System.Threading.RateLimiting;
using besti2.Application.Reviews;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
// Add authentication and authorization
builder.Services.AddAuthorization();
// Login only after the email is confirmed (business owners are confirmed at registration)
builder.Services.AddIdentityApiEndpoints<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        // The e-mail is the user name: allow every character (o'brien@…, bjørn@… must work)
        options.User.AllowedUserNameCharacters = null!;
        // Min. 6 characters with upper and lower case, a digit and a symbol (Identity's default rules)
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddHostedService<BookingAnonymizationJob>();
// Password reset mails from Identity go through our email service
builder.Services.AddTransient<IEmailSender<AppUser>, IdentityEmailSender>();
// The web server tells us the visitor's IP in X-Forwarded-For. Only callers on this machine are trusted
// (the default), so a visitor who sends the header straight to the API cannot pretend to be someone else.
// In Docker the web server has its own address: add it to KnownProxies when deploying.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "For mange forsøk. Prøv igjen om litt." }, cancellationToken);
    // Max 10 attempts per minute per visitor, counted SEPARATELY for login, sign-up and booking
    // (10 wrong passwords must not also block booking for everyone behind the same network)
    foreach (var name in new[] { "login", "signup", "booking" })
    {
        options.AddPolicy(name, httpContext =>
            // Already the visitor's own IP here, thanks to UseForwardedHeaders
            RateLimitPartition.GetFixedWindowLimiter($"{name}:{httpContext.Connection.RemoteIpAddress}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }
    // Max 10 image uploads per minute per account (reading a photo is heavy work for the server)
    options.AddPolicy("upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
var app = builder.Build();
//--------------------------------------
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevUserSeeder.SeedAsync(app.Services);
}

// First: the visitor's real IP for everything after (rate limiting)
app.UseForwardedHeaders();
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
app.UseRateLimiter();

// Get all businesses (optional filters: category, city, text q, date + from/to = free times that day)
app.MapGet("/api/businesses",
    async (string? category, string? city, string? q, DateOnly? date, TimeOnly? from, TimeOnly? to, double? lat,
        double? lng, double? maxKm, int? page,
        IBusinessService businessService, CancellationToken cancellationToken) =>
    {
        var all = await businessService.GetAllAsync(category, city, q, date, from, to, lat, lng, maxKm, cancellationToken);
        // 24 businesses per page (?page=2 ...)
        return Results.Ok(BusinessPageDto.From(all, page));
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
    }).RequireAuthorization().RequireRateLimiting("booking");

// Register a new business with its owner account
app.MapPost("/api/businesses/register", async (RegisterBusinessRequest request,
    IBusinessRegistrationService registrationService, CancellationToken cancellationToken) =>
{
    var result = await registrationService.RegisterAsync(request, cancellationToken);
    return result.Success
        ? Results.Created()
        : Results.BadRequest(new { message = result.Error });
}).RequireRateLimiting("signup");

// Customer sign-up: creates the account and sends a confirmation mail
app.MapPost("/api/account/register", async (RegisterCustomerRequest request,
    ICustomerAccountService accountService, CancellationToken cancellationToken) =>
{
    var error = await accountService.RegisterAsync(request, cancellationToken);
    return error is null ? Results.Created() : Results.BadRequest(new { message = error });
}).RequireRateLimiting("signup");

// Customer clicks the link in the mail; the web page sends the id and code here
app.MapPost("/api/account/confirm-email", async (ConfirmEmailRequest request,
    ICustomerAccountService accountService, IBusinessRegistrationService registrationService) =>
{
    var confirmed = await accountService.ConfirmEmailAsync(request);
    if (confirmed)
    {
        // A business owner: the business now shows up for customers
        await registrationService.ShowBusinessOfOwnerAsync(request.UserId);
    }
    return confirmed
        ? Results.NoContent()
        : Results.BadRequest(new { message = "Lenken er ugyldig eller utløpt." });
}).RequireRateLimiting("signup");

// Who is logged in: the web uses isBusiness to choose between /panel and the customer pages
app.MapGet("/api/me", async (ClaimsPrincipal principal, UserManager<AppUser> userManager) =>
{
    var user = await userManager.GetUserAsync(principal);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new { user.Email, user.FullName, IsBusiness = user.BusinessId is not null });
}).RequireAuthorization();

// Customer: own bookings for "Mine bestillinger"
app.MapGet("/api/me/bookings", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
    IBookingService bookingService, CancellationToken cancellationToken) =>
{
    var userId = userManager.GetUserId(principal);
    return userId is null
        ? Results.Unauthorized()
        : Results.Ok(await bookingService.GetForCustomerAsync(userId, cancellationToken));
}).RequireAuthorization();

// Customer: review a past booking
app.MapPost("/api/me/reviews", async (CreateReviewRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IReviewService reviewService, CancellationToken cancellationToken) =>
{
    var userId = userManager.GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }
    var error = await reviewService.CreateAsync(userId, request, cancellationToken);
    return error is null ? Results.Created() : Results.BadRequest(new { message = error });
}).RequireAuthorization();

// Public: reviews of a business, newest first, with the average
app.MapGet("/api/businesses/{id:guid}/reviews", async (Guid id, IReviewService reviewService,
        CancellationToken cancellationToken) =>
    Results.Ok(await reviewService.GetForBusinessAsync(id, cancellationToken)));
// Business panel: the owner's reviews
app.MapGet("/api/panel/reviews", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
    IReviewService reviewService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);

    return user?.BusinessId is Guid businessId
        ? Results.Ok(await reviewService.GetForBusinessAsync(businessId, cancellationToken))
        : Results.Forbid();
}).RequireAuthorization();
// Business panel: reply to a review (cannot delete it)
app.MapPost("/api/panel/reviews/{id:guid}/reply", async (Guid id, ReplyToReviewRequest request,
    ClaimsPrincipal principal, UserManager<AppUser> userManager, IReviewService reviewService,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }
    var error = await reviewService.ReplyAsync(businessId, id, request.Reply, cancellationToken);

    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization();


// Customer: cancel own booking before it starts
app.MapPost("/api/me/bookings/{id:guid}/avbestill", async (Guid id, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
{
    var userId = userManager.GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    return await bookingService.CancelByCustomerAsync(id, userId, cancellationToken) switch
    {
        UpdateBookingStatusOutcome.Updated => Results.NoContent(),
        UpdateBookingStatusOutcome.InvalidTransition => Results.BadRequest(
            new { message = "Bestillingen kan ikke avbestilles lenger." }),
        _ => Results.NotFound()
    };
}).RequireAuthorization();
// Customer: own booking to another free time ("Endre tid" in Min Besti)
app.MapPost("/api/me/bookings/{id:guid}/endre-tid", async (Guid id, ChangeTimeRequest request,
    ClaimsPrincipal principal, UserManager<AppUser> userManager, IBookingService bookingService,
    CancellationToken cancellationToken) =>
{
    var userId = userManager.GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var result = await bookingService.ChangeTimeByCustomerAsync(id, userId, request, cancellationToken);
    return result.Outcome switch
    {
        MoveBookingOutcome.Moved => Results.NoContent(),
        MoveBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
        MoveBookingOutcome.SlotTaken => Results.Conflict(
            new { message = "Tiden er ikke lenger ledig. Velg en annen tid." }),
        _ => Results.NotFound()
    };
}).RequireAuthorization();
// ?from=2026-10-20&to=2026-10-26 = the days shown in the calendar (Oslo time); without: the list
app.MapGet("/api/panel/bookings", async (DateOnly? from, DateOnly? to, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    // A calendar shows at most about two months at once
    if (from is DateOnly first && to is DateOnly last && (last < first || last.DayNumber - first.DayNumber > 62))
    {
        return Results.BadRequest(new { message = "Ugyldig periode." });
    }

    return Results.Ok(await bookingService.GetForBusinessAsync(businessId, from, to, cancellationToken));
}).RequireAuthorization();

// Business panel: the business adds a visit itself (phone call, walk-in); confirmed at once
app.MapPost("/api/panel/bookings", async (CreateManualBookingRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var result = await bookingService.CreateByBusinessAsync(businessId, request, cancellationToken);
    return result.Outcome switch
    {
        CreateBookingOutcome.Created => Results.Created($"/api/panel/bookings/{result.Booking!.Id}", result.Booking),
        CreateBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
        CreateBookingOutcome.SlotTaken => Results.Conflict(
            new { message = "Den ansatte har allerede en time da. Velg et annet tidspunkt." }),
        _ => Results.NotFound()
    };
}).RequireAuthorization();
// Business panel: blocked time (holiday, doctor, closed day) for the calendar days from-to
app.MapGet("/api/panel/timeoff", async (DateOnly from, DateOnly to, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, ITimeOffService timeOffService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    if (to < from || to.DayNumber - from.DayNumber > 62)
    {
        return Results.BadRequest(new { message = "Ugyldig periode." });
    }

    return Results.Ok(await timeOffService.GetForBusinessAsync(businessId, from, to, cancellationToken));
}).RequireAuthorization();

// Business panel: block time for one employee or the whole business (visits already there are kept)
app.MapPost("/api/panel/timeoff", async (CreateTimeOffRequest request, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, ITimeOffService timeOffService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var result = await timeOffService.CreateAsync(businessId, request, cancellationToken);
    if (result is null)
    {
        return Results.NotFound();
    }

    return result.Error is null
        ? Results.Created($"/api/panel/timeoff/{result.TimeOff!.Id}", result)
        : Results.BadRequest(new { message = result.Error });
}).RequireAuthorization();

// Business panel: remove blocked time
app.MapDelete("/api/panel/timeoff/{id:guid}", async (Guid id, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, ITimeOffService timeOffService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    return await timeOffService.DeleteAsync(businessId, id, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound();
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
}).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("upload");

// Business panel: add a photo to the gallery
app.MapPost("/api/panel/gallery", async (IFormFile file, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessImageService imageService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    await using var stream = file.OpenReadStream();
    var error = await imageService.AddGalleryImageAsync(businessId, stream, file.Length, cancellationToken);
    return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("upload");
// Business panel: delete a gallery photo
app.MapDelete("/api/panel/gallery/{id:guid}", async (Guid id, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBusinessImageService imageService, CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    return await imageService.DeleteGalleryImageAsync(businessId, id, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound();
}).RequireAuthorization();

// Identity endpoints: we only use login, forgotPassword and resetPassword.
// Everything else (register, manage, confirmEmail, refresh ...) answers 404,
// also with a "/" at the end or in other letter case (/api/auth/Register/)
string[] allowedAuthPaths = ["/api/auth/login", "/api/auth/forgotPassword", "/api/auth/resetPassword"];
var auth = app.MapGroup("/api/auth").RequireRateLimiting("login");
auth.AddEndpointFilter(async (context, next) =>
{
    var path = context.HttpContext.Request.Path.Value?.TrimEnd('/') ?? "";
    return allowedAuthPaths.Contains(path, StringComparer.OrdinalIgnoreCase)
        ? await next(context)
        : Results.NotFound();
});
auth.MapIdentityApi<AppUser>();

// Shared logic for panel status changes (confirm / cancel)
async Task<IResult> ChangeBookingStatusAsync(Guid id, BookingStatus newStatus, string? message, ClaimsPrincipal principal,
    UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken)
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var outcome = await bookingService.UpdateStatusAsync(id, businessId, newStatus, message, cancellationToken);

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
        ChangeBookingStatusAsync(id, BookingStatus.Bekreftet, null, principal, userManager, bookingService,
            cancellationToken))
    .RequireAuthorization();

/// Cancel a booking; the body { message } is optional
app.MapPost("/api/panel/bookings/{id:guid}/avlys", (Guid id, CancelBookingRequest? request, ClaimsPrincipal principal,
        UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
    ChangeBookingStatusAsync(id, BookingStatus.Avlyst, request?.Message, principal, userManager, bookingService,
        cancellationToken))
    .RequireAuthorization();

// Move a visit to another time and/or employee
app.MapPost("/api/panel/bookings/{id:guid}/flytt", async (Guid id, MoveBookingRequest request,
    ClaimsPrincipal principal, UserManager<AppUser> userManager, IBookingService bookingService,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user?.BusinessId is not Guid businessId)
    {
        return Results.Forbid();
    }

    var result = await bookingService.MoveAsync(id, businessId, request, cancellationToken);
    return result.Outcome switch
    {
        MoveBookingOutcome.Moved => Results.NoContent(),
        MoveBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
        MoveBookingOutcome.SlotTaken => Results.Conflict(
            new { message = "Den ansatte har allerede en time da. Velg et annet tidspunkt." }),
        _ => Results.NotFound()
    };
}).RequireAuthorization();
app.Run();