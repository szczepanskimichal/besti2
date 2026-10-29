using System.Text;
using besti2.Application.Bookings;
using besti2.Application.Customers;

namespace besti2.Endpoints.Panel;

// "Kunder": the business's customers with history and the business's own note, and the CSV export
public static class PanelCustomerEndpoints
{
    public static void MapPanelCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/customers", async (string? q, PanelBusiness business, ICustomerCardService customers,
                CancellationToken cancellationToken) =>
            Results.Ok(await customers.ListAsync(business.Id, q, DateTime.UtcNow, cancellationToken)));

        group.MapGet("/customers/{key}", async (string key, PanelBusiness business, ICustomerCardService customers,
            CancellationToken cancellationToken) =>
        {
            var card = await customers.GetAsync(business.Id, key, DateTime.UtcNow, cancellationToken);
            return card is null ? Results.NotFound() : Results.Ok(card);
        });

        group.MapPut("/customers/{key}/note", async (string key, CustomerNoteRequest request, PanelBusiness business,
            ICustomerCardService customers, CancellationToken cancellationToken) =>
        {
            var error = await customers.SetNoteAsync(business.Id, key, request.Note, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        // from/to = days in Oslo (yyyy-MM-dd); a file with a BOM, so Excel shows æ, ø and å right
        group.MapGet("/export/bookings", async (DateOnly from, DateOnly to, PanelBusiness business,
            IBookingExportService export, CancellationToken cancellationToken) =>
        {
            var csv = await export.BookingsCsvAsync(business.Id, from, to, cancellationToken);
            return csv is null
                ? Results.BadRequest(new { message = "Velg en periode på maks to år." })
                : Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
                    "text/csv; charset=utf-8", $"bestillinger-{from:yyyy-MM-dd}-{to:yyyy-MM-dd}.csv");
        });
    }
}
