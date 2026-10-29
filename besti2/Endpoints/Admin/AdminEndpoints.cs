using besti2.Application.Admin;
using besti2.Application.Businesses;

namespace besti2.Endpoints.Admin;

// /admin on the web: the BESTI team approves businesses, follows them up and reads new reviews
public static class AdminEndpoints
{
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

        group.MapPut("/businesses/{id:guid}/note", async (Guid id, AdminNoteRequest request,
            IAdminService adminService, CancellationToken cancellationToken) =>
        {
            var error = await adminService.SetNoteAsync(id, request.Note, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // Same as the one-click link in the e-mail: the owner gets the "godkjent" mail
        group.MapPost("/businesses/{id:guid}/approve", async (Guid id,
                IBusinessRegistrationService registrationService, CancellationToken cancellationToken) =>
            await registrationService.ApproveByIdAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        // Deletes the registration and the owner account (only while it waits for approval)
        group.MapPost("/businesses/{id:guid}/reject", async (Guid id,
                IBusinessRegistrationService registrationService, CancellationToken cancellationToken) =>
            await registrationService.RejectByIdAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        group.MapGet("/reviews", async (IAdminService adminService, CancellationToken cancellationToken) =>
            Results.Ok(await adminService.GetLatestReviewsAsync(50, cancellationToken)));
    }
}
