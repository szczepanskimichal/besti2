using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Bookings;
using besti2.Application.Staff;
using besti2.Infrastructure.Identity;

namespace besti2.Endpoints;

// /ansatt on the web: an employee with their own BESTI account sees only their own visits
public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/staff");

        // The link in the invitation e-mail, before logging in
        group.MapGet("/invites/{token}", async (string token, IStaffService staffService,
            CancellationToken cancellationToken) =>
        {
            var invite = await staffService.GetInviteAsync(token, DateTime.UtcNow, cancellationToken);
            return invite is null ? Results.NotFound() : Results.Ok(invite);
        }).RequireRateLimiting("search");

        group.MapPost("/invites/{token}/accept", async (string token, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IStaffService staffService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var error = await staffService.AcceptAsync(userId, token, DateTime.UtcNow, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization().RequireRateLimiting("login");

        // from = the first day (Oslo), days = 1–14
        group.MapGet("/schedule", async (DateOnly? from, int? days, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IStaffService staffService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                SlotCalculator.Oslo));
            return Results.Ok(await staffService.GetScheduleAsync(userId, from ?? today, days ?? 7,
                cancellationToken));
        }).RequireAuthorization();

        group.MapPost("/places/{employeeId:guid}/leave", async (Guid employeeId, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IStaffService staffService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is not null && await staffService.LeaveAsync(userId, employeeId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }).RequireAuthorization();
    }
}
