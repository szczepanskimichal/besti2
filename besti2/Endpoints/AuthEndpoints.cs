using besti2.Application.Accounts;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Identity endpoints: we only use login, forgotPassword and resetPassword (and our own google).
        // Everything else (register, manage, confirmEmail, refresh ...) answers 404,
        // also with a "/" at the end or in other letter case (/api/auth/Register/)
        string[] allowedAuthPaths =
            ["/api/auth/login", "/api/auth/forgotPassword", "/api/auth/resetPassword", "/api/auth/google"];
        var auth = app.MapGroup("/api/auth").RequireRateLimiting("login");
        auth.AddEndpointFilter(async (context, next) =>
        {
            var path = context.HttpContext.Request.Path.Value?.TrimEnd('/') ?? "";
            return allowedAuthPaths.Contains(path, StringComparer.OrdinalIgnoreCase)
                ? await next(context)
                : Results.NotFound();
        });
        auth.MapIdentityApi<AppUser>();

        // "Fortsett med Google": a Google ID token in, the same login token as /login out
        auth.MapPost("/google", async (GoogleLoginRequest request, IGoogleLoginService googleLogin,
            UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            CancellationToken cancellationToken) =>
        {
            var result = await googleLogin.FindOrCreateUserAsync(request.IdToken, cancellationToken);
            var user = result.UserId is null ? null : await userManager.FindByIdAsync(result.UserId);
            if (user is null)
            {
                return Results.BadRequest(new { message = result.Error });
            }

            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            await signInManager.SignInAsync(user, isPersistent: false);
            // The token is already written by the sign-in
            return Results.Empty;
        });
    }
}