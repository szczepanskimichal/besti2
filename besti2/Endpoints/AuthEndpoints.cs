using besti2.Infrastructure.Identity;

namespace besti2.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Identity endpoints: we only use login, forgotPassword and resetPassword.
        // Everything else (register, manage, confirmEmail, refresh ...) answers 404,
        // also with a "/" at the end or in other letter case (/api/auth/Register/)
        string[] allowedAuthPaths = ["/api/auth/login", "/api/auth/forgotPassword", "/api/auth/resetPassword"];
        var auth = app.MapGroup("/api/auth").RequireRateLimiting("login");
        auth.AddEndpointFilter(async (context, next) =>
        {
            var path = context.HttpContext.Request.Path.Value?.TrimEnd('/') ?? "";
            return allowedAuthPaths.Contains(path, StringComparer.OrdinalIgnoreCase)
                ? await next(context)
                : Results.NotFound();
        });
        auth.MapIdentityApi<AppUser>();
    }
}
