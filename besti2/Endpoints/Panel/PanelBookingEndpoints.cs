using besti2.Application.Bookings;
using besti2.Domain.Enums;

namespace besti2.Endpoints.Panel;

// The calendar: visits and blocked time
public static class PanelBookingEndpoints
{
    public static void MapPanelBookingEndpoints(this RouteGroupBuilder group)
    {
        // ?from=2026-10-20&to=2026-10-26 = the days shown in the calendar (Oslo time); without: the list
        group.MapGet("/bookings", async (DateOnly? from, DateOnly? to, PanelBusiness business,
            IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            // A calendar shows at most about two months at once
            if (from is DateOnly first && to is DateOnly last &&
                (last < first || last.DayNumber - first.DayNumber > 62))
            {
                return Results.BadRequest(new { message = "Ugyldig periode." });
            }

            return Results.Ok(await bookingService.GetForBusinessAsync(business.Id, from, to, cancellationToken));
        });

        // Business panel: the business adds a visit itself (phone call, walk-in); confirmed at once
        group.MapPost("/bookings", async (CreateManualBookingRequest request, PanelBusiness business,
            IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var result = await bookingService.CreateByBusinessAsync(business.Id, request, cancellationToken);
            return result.Outcome switch
            {
                CreateBookingOutcome.Created => Results.Created($"/api/panel/bookings/{result.Booking!.Id}",
                    result.Booking),
                CreateBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
                CreateBookingOutcome.SlotTaken => Results.Conflict(
                    new { message = "Den ansatte har allerede en time da. Velg et annet tidspunkt." }),
                _ => Results.NotFound()
            };
        });

        // Confirm a booking
        group.MapPost("/bookings/{id:guid}/bekreft", (Guid id, PanelBusiness business,
                IBookingService bookingService, CancellationToken cancellationToken) =>
            ChangeBookingStatusAsync(id, BookingStatus.Bekreftet, null, business, bookingService, cancellationToken));

        // Cancel a booking; the body { message } is optional
        group.MapPost("/bookings/{id:guid}/avlys", (Guid id, CancelBookingRequest? request, PanelBusiness business,
                IBookingService bookingService, CancellationToken cancellationToken) =>
            ChangeBookingStatusAsync(id, BookingStatus.Avlyst, request?.Message, business, bookingService,
                cancellationToken));

        // The customer did not come (only after the start time)
        group.MapPost("/bookings/{id:guid}/motte-ikke", (Guid id, PanelBusiness business,
                IBookingService bookingService, CancellationToken cancellationToken) =>
            ChangeBookingStatusAsync(id, BookingStatus.MotteIkke, null, business, bookingService, cancellationToken));

        // Move a visit to another time and/or employee
        group.MapPost("/bookings/{id:guid}/flytt", async (Guid id, MoveBookingRequest request,
            PanelBusiness business, IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var result = await bookingService.MoveAsync(id, business.Id, request, cancellationToken);
            return result.Outcome switch
            {
                MoveBookingOutcome.Moved => Results.NoContent(),
                MoveBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
                MoveBookingOutcome.SlotTaken => Results.Conflict(
                    new { message = "Den ansatte har allerede en time da. Velg et annet tidspunkt." }),
                _ => Results.NotFound()
            };
        });

        // Business panel: blocked time (holiday, doctor, closed day) for the calendar days from-to
        group.MapGet("/timeoff", async (DateOnly from, DateOnly to, PanelBusiness business,
            ITimeOffService timeOffService, CancellationToken cancellationToken) =>
        {
            if (to < from || to.DayNumber - from.DayNumber > 62)
            {
                return Results.BadRequest(new { message = "Ugyldig periode." });
            }

            return Results.Ok(await timeOffService.GetForBusinessAsync(business.Id, from, to, cancellationToken));
        });

        // Business panel: block time for one employee or the whole business (visits already there are kept)
        group.MapPost("/timeoff", async (CreateTimeOffRequest request, PanelBusiness business,
            ITimeOffService timeOffService, CancellationToken cancellationToken) =>
        {
            var result = await timeOffService.CreateAsync(business.Id, request, cancellationToken);
            if (result is null)
            {
                return Results.NotFound();
            }

            return result.Error is null
                ? Results.Created($"/api/panel/timeoff/{result.TimeOff!.Id}", result)
                : Results.BadRequest(new { message = result.Error });
        });

        // Business panel: remove blocked time
        group.MapDelete("/timeoff/{id:guid}", async (Guid id, PanelBusiness business,
                ITimeOffService timeOffService, CancellationToken cancellationToken) =>
            await timeOffService.DeleteAsync(business.Id, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());
    }

    // Shared logic for panel status changes (confirm / cancel / did not come)
    private static async Task<IResult> ChangeBookingStatusAsync(Guid id, BookingStatus newStatus, string? message,
        PanelBusiness business, IBookingService bookingService, CancellationToken cancellationToken)
    {
        var outcome = await bookingService.UpdateStatusAsync(id, business.Id, newStatus, message, cancellationToken);

        return outcome switch
        {
            UpdateBookingStatusOutcome.Updated => Results.NoContent(),
            UpdateBookingStatusOutcome.InvalidTransition =>
                Results.Conflict(new { message = "Bestillingen kan ikke endres til denne statusen." }),
            _ => Results.NotFound()
        };
    }
}