using besti2.Application.Images;

namespace besti2.Endpoints.Panel;

public static class PanelMediaEndpoints
{
    public static void MapPanelMediaEndpoints(this RouteGroupBuilder group)
    {
        // Business panel: owner uploads a logo (PNG, JPG or WebP, max 10 MB, saved as small WebP)
        group.MapPost("/logo", async (IFormFile file, PanelBusiness business, IBusinessImageService imageService,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            var error = await imageService.SetLogoAsync(business.Id, stream, file.Length, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).DisableAntiforgery().RequireRateLimiting("upload");

        // Business panel: add a photo to the gallery
        group.MapPost("/gallery", async (IFormFile file, PanelBusiness business, IBusinessImageService imageService,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            var error = await imageService.AddGalleryImageAsync(business.Id, stream, file.Length, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).DisableAntiforgery().RequireRateLimiting("upload");

        // Business panel: delete a gallery photo
        group.MapDelete("/gallery/{id:guid}", async (Guid id, PanelBusiness business,
                IBusinessImageService imageService, CancellationToken cancellationToken) =>
            await imageService.DeleteGalleryImageAsync(business.Id, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());
    }
}