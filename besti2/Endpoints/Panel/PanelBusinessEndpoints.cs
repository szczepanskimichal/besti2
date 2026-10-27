using besti2.Application.Businesses;

namespace besti2.Endpoints.Panel;

// The owner's own business: profile, opening hours, services and employees
public static class PanelBusinessEndpoints
{
    public static void MapPanelBusinessEndpoints(this RouteGroupBuilder group)
    {
        // Business panel: the owner's own business with services and employees
        group.MapGet("/business", async (PanelBusiness business, IBusinessService businessService,
            CancellationToken cancellationToken) =>
        {
            var details = await businessService.GetForPanelAsync(business.Id, cancellationToken);
            return details is null ? Results.NotFound() : Results.Ok(details);
        });

        // Business panel: owner edits description and contact info
        group.MapPut("/business", async (UpdateProfileRequest request, PanelBusiness business,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var error = await businessService.UpdateProfileAsync(business.Id, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // Business panel: owner updates opening hours
        group.MapPut("/opening-hours", async (UpdateOpeningHoursRequest request, PanelBusiness business,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var error = await businessService.UpdateOpeningHoursAsync(business.Id, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // Business panel: add a service to the owner's business
        group.MapPost("/services", async (CreateServiceRequest request, PanelBusiness business,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var error = await businessService.AddServiceAsync(business.Id, request, cancellationToken);
            return error is null ? Results.Created() : Results.BadRequest(new { message = error });
        });

        // Business panel: edit one of the owner's services
        group.MapPut("/services/{id:guid}", async (Guid id, CreateServiceRequest request, PanelBusiness business,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var error = await businessService.UpdateServiceAsync(business.Id, id, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // Business panel: soft delete one of the owner's services
        group.MapDelete("/services/{id:guid}", async (Guid id, PanelBusiness business,
                IBusinessService businessService, CancellationToken cancellationToken) =>
            await businessService.DeleteServiceAsync(business.Id, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        // Business panel: add an employee
        group.MapPost("/employees", async (EmployeeRequest request, PanelBusiness business,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var error = await businessService.AddEmployeeAsync(business.Id, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // Business panel: edit one of the owner's employees
        group.MapPut("/employees/{id:guid}", async (Guid id, EmployeeRequest request, PanelBusiness business,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var error = await businessService.UpdateEmployeeAsync(business.Id, id, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // Business panel: soft delete one of the owner's employees (never the last one)
        group.MapDelete("/employees/{id:guid}", async (Guid id, PanelBusiness business,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var error = await businessService.DeleteEmployeeAsync(business.Id, id, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });
    }
}