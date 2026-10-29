using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using besti2.Application.Admin;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Tests;

// /admin: only the e-mails in Admin:Emails get in; approvals, the business list and notes
public class AdminTests
{
    private static readonly DateTime Now = new(2030, 1, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    // check if a business waiting for approval is listed first and counted, with its owner's e-mail
    [Fact]
    public async Task Overview_ListsWaitingBusiness()
    {
        await using var db = await CreateSeededDbAsync();
        var waiting = await db.Businesses.FindAsync(KlippOgStil);
        waiting!.IsHidden = true;
        waiting.ApprovalTokenHash = "hash";
        db.Users.Add(new AppUser { UserName = "eier@test.example", Email = "eier@test.example", BusinessId = KlippOgStil });
        db.Users.Add(new AppUser { UserName = "kunde@test.example", Email = "kunde@test.example" });
        await db.SaveChangesAsync();
        var service = new AdminService(db);

        var overview = await service.GetOverviewAsync(Now);
        var list = await service.GetBusinessesAsync(null, "eier@", Now);

        Assert.Equal(1, overview.WaitingForApproval);
        Assert.Equal(2, overview.VisibleBusinesses);
        Assert.Equal(1, overview.CustomerAccounts);
        var row = Assert.Single(overview.Waiting);
        Assert.Equal("eier@test.example", row.OwnerEmail);
        Assert.Equal("Venter", row.Status);
        Assert.Equal(KlippOgStil, Assert.Single(list).Id);
    }

    // check if the note is saved, emptied and limited in length
    [Fact]
    public async Task SetNote_SavedAndLimited()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new AdminService(db);

        Assert.Null(await service.SetNoteAsync(KlippOgStil, "  Ringt 29.10, vil ha hjelp med bilder  "));
        var details = await service.GetBusinessAsync(KlippOgStil, Now);
        Assert.NotNull(await service.SetNoteAsync(KlippOgStil, new string('x', 2001)));
        Assert.NotNull(await service.SetNoteAsync(Guid.NewGuid(), "x"));

        Assert.Equal("Ringt 29.10, vil ha hjelp med bilder", details!.AdminNote);
        Assert.True(details.Business.HasNote);
        Assert.Contains(details.Setup, s => s.Title == "Åpningstider" && s.Done);
    }

    // check if only an admin e-mail gets /api/admin, sees isAdmin, and can approve a waiting business
    [Fact]
    public async Task AdminApi_OnlyForAdminEmails()
    {
        await using var factory = new ApiFactory();
        await using var configured = factory.WithWebHostBuilder(b => b.UseSetting("Admin:Emails", "ada@test.example, michal@test.example"));
        using (var scope = configured.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            var waiting = await db.Businesses.FindAsync(KlippOgStil);
            waiting!.IsHidden = true;
            waiting.ApprovalTokenHash = "hash";
            await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            foreach (var email in new[] { "ada@test.example", "kunde@test.example" })
            {
                Assert.True((await users.CreateAsync(
                    new AppUser { UserName = email, Email = email, EmailConfirmed = true }, "Test123!")).Succeeded);
            }
        }

        var ada = await LoginAsync(configured, "ada@test.example");
        var customer = await LoginAsync(configured, "kunde@test.example");
        var adminMe = await ada.GetFromJsonAsync<MeResponse>("/api/me");
        var customerMe = await customer.GetFromJsonAsync<MeResponse>("/api/me");
        var forCustomer = await customer.GetAsync("/api/admin/overview");
        var withoutLogin = await configured.CreateClient().GetAsync("/api/admin/overview");
        var overview = await ada.GetFromJsonAsync<AdminOverviewDto>("/api/admin/overview");
        var approve = await ada.PostAsync($"/api/admin/businesses/{KlippOgStil}/approve", null);
        var approveAgain = await ada.PostAsync($"/api/admin/businesses/{KlippOgStil}/approve", null);

        Assert.True(adminMe!.IsAdmin);
        Assert.False(customerMe!.IsAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, forCustomer.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withoutLogin.StatusCode);
        Assert.Equal(1, overview!.WaitingForApproval);
        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, approveAgain.StatusCode);
        using var check = configured.Services.CreateScope();
        var business = await check.ServiceProvider.GetRequiredService<AppDbContext>().Businesses.FindAsync(KlippOgStil);
        Assert.False(business!.IsHidden);
        Assert.NotNull(business.ApprovedUtc);
    }

    private static async Task<HttpClient> LoginAsync(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory,
        string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test123!" });
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record TokenResponse(string AccessToken);

    private record MeResponse(bool IsAdmin);
}
