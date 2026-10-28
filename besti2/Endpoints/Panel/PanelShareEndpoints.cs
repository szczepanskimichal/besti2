using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace besti2.Endpoints.Panel;

// "Del og få kunder": the QR code to the business page, for the poster, the counter and Instagram
public static class PanelShareEndpoints
{
    public static void MapPanelShareEndpoints(this RouteGroupBuilder group)
    {
        // format=svg (sharp at any size, for printing) or png (for Instagram, Facebook and Word)
        group.MapGet("/qr", async (string? format, PanelBusiness business, AppDbContext dbContext,
            IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var slug = await dbContext.Businesses.Where(b => b.Id == business.Id).Select(b => b.Slug)
                .FirstOrDefaultAsync(cancellationToken);
            if (slug is null)
            {
                return Results.NotFound();
            }

            // ?utm_source=qr: the visit statistics show how many came from the poster
            var url = $"{configuration["WebBaseUrl"] ?? "http://localhost:3000"}/{slug}?utm_source=qr";
            // Level M: still readable with a scratch or a bit of tape over the poster
            using var data = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.M);
            return format == "png"
                ? Results.File(new PngByteQRCode(data).GetGraphic(20), "image/png", "besti-qr.png")
                : Results.Text(new SvgQRCode(data).GetGraphic(10), "image/svg+xml");
        });
    }
}
