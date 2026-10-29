using besti2.Application.Staff;

namespace besti2.Endpoints.Panel;

// Panel → Ansatte → "Egen innlogging": the owner gives an employee access to their own calendar
public static class PanelStaffEndpoints
{
    public static void MapPanelStaffEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/staff", async (PanelBusiness business, IStaffService staffService,
                CancellationToken cancellationToken) =>
            Results.Ok(await staffService.GetAccessAsync(business.Id, cancellationToken)));

        group.MapPost("/employees/{id:guid}/staff", async (Guid id, StaffInviteRequest request,
            PanelBusiness business, IStaffService staffService, CancellationToken cancellationToken) =>
        {
            var error = await staffService.InviteAsync(business.Id, id, request.Email, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireRateLimiting("signup");

        group.MapDelete("/employees/{id:guid}/staff", async (Guid id, PanelBusiness business,
                IStaffService staffService, CancellationToken cancellationToken) =>
            await staffService.RemoveAsync(business.Id, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());
    }
}
