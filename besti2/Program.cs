using besti2.Endpoints;
using besti2.Endpoints.Panel;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using besti2.Infrastructure;
using besti2.Identity;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Jobs;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
// Add authentication and authorization
builder.Services.AddAuthorization(options =>
    options.AddPolicy(PanelBusiness.Policy, policy => policy.AddRequirements(new BusinessOwnerRequirement())));
builder.Services.AddScoped<IAuthorizationHandler, BusinessOwnerHandler>();
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
// Logged in for 14 days (a salon has the panel open all day); a password change ends it everywhere (see below)
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
    options.BearerTokenExpiration = TimeSpan.FromDays(14));
builder.Services.AddHostedService<BookingAnonymizationJob>();
builder.Services.AddHostedService<BookingReminderJob>();
builder.Services.AddHostedService<EmailSenderJob>();
builder.Services.AddHostedService<BusinessVerificationJob>();
// Password reset mails from Identity go through our email service
builder.Services.AddTransient<IEmailSender<AppUser>, IdentityEmailSender>();
// The web server tells us the visitor's IP in X-Forwarded-For. Only callers on this machine are trusted
// (the default), so a visitor who sends the header straight to the API cannot pretend to be someone else.
// In Docker the web server has its own address: the server settings name the Docker network to trust.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    var knownNetwork = builder.Configuration["ForwardedHeaders:KnownNetwork"];
    if (!string.IsNullOrEmpty(knownNetwork))
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(knownNetwork));
    }
});
// Login tokens and e-mail links are signed with these keys. On the server they are kept on a volume,
// otherwise every restart would log everyone out and break the links in e-mails already sent
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("besti2")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

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
    // Search and free times: max 60 per minute per visitor (normal clicking never gets close, a script does)
    options.AddPolicy("search", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter($"search:{httpContext.Connection.RemoteIpAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

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
// The same search (e.g. the home page or "Frisør i Gjøvik") is answered from memory for 30 seconds:
// thousands of visitors then cost the database one query instead of thousands
builder.Services.AddOutputCache(options =>
    options.AddPolicy("search", policy => policy.Expire(TimeSpan.FromSeconds(30)).SetVaryByQuery("*")));
var app = builder.Build();
//--------------------------------------
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevUserSeeder.SeedAsync(app.Services);
}

// On the server: bring the database up to date and remove the demo businesses (flags in the server settings)
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    if (app.Configuration.GetValue<bool>("Database:RemoveDemoData"))
    {
        await DemoData.RemoveAsync(dbContext);
    }
}

// First: the visitor's real IP for everything after (rate limiting)
app.UseForwardedHeaders();
// Uploaded images (development storage), e.g. /uploads/abc.webp
var uploadsFolder = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsFolder);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsFolder),
    RequestPath = "/uploads"
});
app.UseAuthentication();
// A login from before a password change or reset stops working on every device
// (Identity gives the account a new security stamp then; a deleted account has none)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var signInManager = context.RequestServices.GetRequiredService<SignInManager<AppUser>>();
        if (await signInManager.ValidateSecurityStampAsync(context.User) is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }

    await next();
});
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
// For Docker and the deploy check: 200 when the API and its database answer
app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken) ? Results.Ok() : Results.StatusCode(503));

app.MapBusinessEndpoints();
app.MapBookingEndpoints();
app.MapAccountEndpoints();
app.MapGodkjennEndpoints();
app.MapNotificationEndpoints();
app.MapPanelEndpoints();
app.MapAuthEndpoints();


app.Run();