using besti2.Application.Reviews;

namespace besti2.Endpoints.Panel;

public static class PanelReviewEndpoints
{
    public static void MapPanelReviewEndpoints(this RouteGroupBuilder group)
    {
        // Business panel: the owner's reviews
        group.MapGet("/reviews", async (PanelBusiness business, IReviewService reviewService,
                CancellationToken cancellationToken) =>
            Results.Ok(await reviewService.GetForBusinessAsync(business.Id, cancellationToken)));

        // Business panel: reply to a review (cannot delete it)
        group.MapPost("/reviews/{id:guid}/reply", async (Guid id, ReplyToReviewRequest request,
            PanelBusiness business, IReviewService reviewService, CancellationToken cancellationToken) =>
        {
            var error = await reviewService.ReplyAsync(business.Id, id, request.Reply, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });
    }
}