using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Infrastructure.Identity;

public static class DevUserSeeder
{
    // Development only: one owner account per seeded business
    private const string DevPassword = "Test123!";

    private static readonly (string Email, Guid BusinessId)[] Owners =
    [
        ("eier@klippogstil.example", new Guid("11111111-1111-1111-1111-111111111111")),
        ("eier@fjordbygg.example", new Guid("22222222-2222-2222-2222-222222222222")),
        ("eier@nordlyspsykolog.example", new Guid("33333333-3333-3333-3333-333333333333")),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        foreach (var (email, businessId) in Owners)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new AppUser { UserName = email, Email = email, BusinessId = businessId };
            var result = await userManager.CreateAsync(user, DevPassword);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}