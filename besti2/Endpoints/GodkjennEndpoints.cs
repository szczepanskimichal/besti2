using besti2.Application.Businesses;

namespace besti2.Endpoints;

public static class GodkjennEndpoints
{
    public static void MapGodkjennEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/godkjenn");

        // BESTI approves a new business: the link in the mail to AdminEmail opens a page that shows it first
        group.MapGet("", async (string token, IBusinessRegistrationService registrationService,
            CancellationToken cancellationToken) =>
        {
            var approval = await registrationService.GetApprovalAsync(token, cancellationToken);
            return approval is null ? Results.NotFound() : Results.Ok(approval);
        }).RequireRateLimiting("signup");

        group.MapPost("", async (ApproveBusinessRequest request, IBusinessRegistrationService registrationService,
                CancellationToken cancellationToken) =>
            await registrationService.ApproveAsync(request.Token, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound()).RequireRateLimiting("signup");

        group.MapPost("/avvis", async (ApproveBusinessRequest request, IBusinessRegistrationService registrationService,
                CancellationToken cancellationToken) =>
            await registrationService.RejectAsync(request.Token, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound()).RequireRateLimiting("signup");
            }
}