using System.Diagnostics;
using System.Security.Claims;
using besti2.Application.Accounts;
using besti2.Application.Admin;
using besti2.Application.Businesses;
using besti2.Infrastructure.Services;

namespace besti2.Endpoints.Admin;

// /admin on the web: the BESTI team approves businesses, follows them up, reads new reviews, helps customers
// and sees how the system is doing. Every change is written to the log with the admin's e-mail
public static class AdminEndpoints
{
    private static readonly DateTime StartedUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization(AdminEmails.Policy);

        group.MapGet("/overview", async (IAdminService adminService, CancellationToken cancellationToken) =>
            Results.Ok(await adminService.GetOverviewAsync(DateTime.UtcNow, cancellationToken)));

        group.MapGet("/businesses", async (string? status, string? q, IAdminService adminService,
                CancellationToken cancellationToken) =>
            Results.Ok(await adminService.GetBusinessesAsync(status, q, DateTime.UtcNow, cancellationToken)));

        group.MapGet("/businesses/{id:guid}", async (Guid id, IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var business = await adminService.GetBusinessAsync(id, DateTime.UtcNow, cancellationToken);
            return business is null ? Results.NotFound() : Results.Ok(business);
        });

        group.MapPut("/businesses/{id:guid}/note", async (Guid id, AdminNoteRequest request, ClaimsPrincipal principal,
            IAdminService adminService, CancellationToken cancellationToken) =>
        {
            var error = await adminService.SetNoteAsync(id, request.Note, cancellationToken);
            if (error is null)
            {
                await adminService.LogAsync(AdminEmail(principal), "Endret notatet", id, cancellationToken);
            }

            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // Same as the one-click link in the e-mail: the owner gets the "godkjent" mail
        group.MapPost("/businesses/{id:guid}/approve", async (Guid id, ClaimsPrincipal principal,
            IBusinessRegistrationService registrationService, IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            if (!await registrationService.ApproveByIdAsync(id, cancellationToken))
            {
                return Results.NotFound();
            }

            var name = (await adminService.GetBusinessAsync(id, DateTime.UtcNow, cancellationToken))?.Business.Name;
            await adminService.LogAsync(AdminEmail(principal), $"Godkjente {name}", id, cancellationToken);
            return Results.NoContent();
        });

        // Deletes the registration and the owner account (only while it waits for approval)
        group.MapPost("/businesses/{id:guid}/reject", async (Guid id, ClaimsPrincipal principal,
            IBusinessRegistrationService registrationService, IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            // The name first: the business is gone afterwards
            var name = (await adminService.GetBusinessAsync(id, DateTime.UtcNow, cancellationToken))?.Business.Name;
            if (!await registrationService.RejectByIdAsync(id, cancellationToken))
            {
                return Results.NotFound();
            }

            await adminService.LogAsync(AdminEmail(principal), $"Avviste og slettet registreringen {name}", id,
                cancellationToken);
            return Results.NoContent();
        });

        // "Steng bedriften" / "Åpne igjen": hidden from customers, the owner is told why
        group.MapPost("/businesses/{id:guid}/suspend", async (Guid id, AdminReasonRequest request,
            ClaimsPrincipal principal, IAdminService adminService, CancellationToken cancellationToken) =>
            ToResult(await adminService.SuspendBusinessAsync(id, request.Reason, AdminEmail(principal),
                cancellationToken)));

        group.MapPost("/businesses/{id:guid}/reopen", async (Guid id, ClaimsPrincipal principal,
                IAdminService adminService, CancellationToken cancellationToken) =>
            ToResult(await adminService.ReopenBusinessAsync(id, AdminEmail(principal), cancellationToken)));

        // skjult=true: only the hidden ones
        group.MapGet("/reviews", async (bool? skjult, IAdminService adminService,
                CancellationToken cancellationToken) =>
            Results.Ok(await adminService.GetLatestReviewsAsync(skjult == true ? 200 : 50, skjult == true,
                cancellationToken)));

        group.MapPost("/reviews/{id:guid}/hide", async (Guid id, AdminReasonRequest request,
            ClaimsPrincipal principal, IAdminService adminService, CancellationToken cancellationToken) =>
            ToResult(await adminService.HideReviewAsync(id, request.Reason, AdminEmail(principal),
                cancellationToken)));

        group.MapPost("/reviews/{id:guid}/show", async (Guid id, ClaimsPrincipal principal,
                IAdminService adminService, CancellationToken cancellationToken) =>
            ToResult(await adminService.ShowReviewAsync(id, AdminEmail(principal), cancellationToken)));

        // The fixed reasons for the two forms above
        group.MapGet("/reasons", () => Results.Ok(new
        {
            review = AdminService.ReviewReasons,
            suspend = AdminService.SuspendReasons
        }));

        // Support: one customer by the exact e-mail or phone number
        group.MapGet("/customers", async (string? q, IAdminService adminService,
                CancellationToken cancellationToken) =>
            Results.Ok(await adminService.FindCustomersAsync(q ?? "", DateTime.UtcNow, cancellationToken)));

        group.MapPost("/customers/{id}/resend-confirmation", async (string id, ClaimsPrincipal principal,
            ICustomerAccountService accountService, IAdminService adminService, CancellationToken cancellationToken) =>
        {
            var error = await accountService.ResendConfirmationAsync(id, cancellationToken);
            if (error is null)
            {
                await adminService.LogAsync(AdminEmail(principal), "Sendte ny bekreftelseslenke til en kunde", null,
                    cancellationToken);
            }

            return ToResult(error);
        });

        // "Systemstatus": version, database, backup, e-mails waiting, background jobs
        group.MapGet("/health", async (IConfiguration configuration, IAdminService adminService,
                CancellationToken cancellationToken) =>
            Results.Ok(await adminService.GetHealthAsync(configuration["BESTI_VERSION"], StartedUtc, DateTime.UtcNow,
                cancellationToken)));

        group.MapGet("/log", async (IAdminService adminService, CancellationToken cancellationToken) =>
            Results.Ok(await adminService.GetLogAsync(200, cancellationToken)));
    }

    // The e-mail is the user name, so it is in the login itself
    private static string AdminEmail(ClaimsPrincipal principal) => principal.Identity?.Name ?? "ukjent";

    private static IResult ToResult(string? error) =>
        error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
}
