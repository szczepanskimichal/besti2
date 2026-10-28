using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Tests;

// Through HTTP: who may call which endpoint and the limits per visitor (service tests do not see these)
public class ApiTests
{
    // check if the API answers the deploy check
    [Fact]
    public async Task Health_Returns200()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // check if the business panel needs a login
    [Fact]
    public async Task Panel_WithoutLogin_Is401()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/panel/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // check if a logged-in customer cannot open the business panel
    [Fact]
    public async Task Panel_AsCustomer_Is403()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser { UserName = "kunde@test.example", Email = "kunde@test.example", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, "Test123!")).Succeeded);
        }

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "kunde@test.example", password = "Test123!" });
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/panel/bookings");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // check if Identity's own sign-up endpoint is hidden (accounts are made only through our endpoints)
    [Fact]
    public async Task IdentityRegister_Is404()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "ny@test.example", password = "Test123!" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // check if the search limit (60 per minute) counts each visitor separately, not the whole site together
    [Fact]
    public async Task Search_LimitIsPerVisitorIp()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 60; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await Search(client, "203.0.113.1")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Search(client, "203.0.113.1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Search(client, "203.0.113.2")).StatusCode);
    }

    // Like the web server: the visitor's IP in X-Forwarded-For
    private static Task<HttpResponseMessage> Search(HttpClient client, string visitorIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/businesses");
        request.Headers.Add("X-Forwarded-For", visitorIp);
        return client.SendAsync(request);
    }

    private record TokenResponse(string AccessToken);
}
