using besti2.Application.Accounts;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Tests;

public class CustomerAccountServiceTests
{
// Real UserManager on an in-memory database, with a token provider for the confirmation code
    private static (CustomerAccountService Service, UserManager<AppUser> Users, FakeEmailService Email) CreateService()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddIdentityCore<AppUser>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddTokenProvider<EmailTokenProvider<AppUser>>(TokenOptions.DefaultProvider);
        var provider = services.BuildServiceProvider();
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var email = new FakeEmailService();
        return (new CustomerAccountService(users, email, "http://localhost:3000"), users, email);
    }
    private static RegisterCustomerRequest TestKunde() => new("Kari Nordmann", "Kari@Test.example", "Test123!");

// check if sign-up creates an unconfirmed customer and mails a confirmation link
    [Fact]
    public async Task RegisterAsync_ValidData_CreatesUnconfirmedCustomerAndSendsMail()
    {
        var (service, users, email) = CreateService();
        var error = await service.RegisterAsync(TestKunde());
        Assert.Null(error);
        var user = await users.FindByEmailAsync("kari@test.example");
        Assert.NotNull(user);
        Assert.Equal("Kari Nordmann", user.FullName);
        Assert.Null(user.BusinessId);
        Assert.False(user.EmailConfirmed);
        var mail = Assert.Single(email.Sent);
        Assert.Equal("kari@test.example", mail.To);
        Assert.Contains("http://localhost:3000/bekreft-epost?id=", mail.Body);
    }

// check if signing up again before confirming sends a new link instead of an error (lost mail)
    [Fact]
    public async Task RegisterAsync_SameEmailTwice_Unconfirmed_SendsNewLink()
    {
        var (service, users, email) = CreateService();
        await service.RegisterAsync(TestKunde());
        var error = await service.RegisterAsync(TestKunde());
        Assert.Null(error);
        Assert.Equal(2, email.Sent.Count);
        Assert.Single(users.Users);
    }

// check if a confirmed email cannot sign up twice
    [Fact]
    public async Task RegisterAsync_SameEmailTwice_Confirmed_SecondFails()
    {
        var (service, users, _) = CreateService();
        await service.RegisterAsync(TestKunde());
        var user = (await users.FindByEmailAsync("kari@test.example"))!;
        await users.ConfirmEmailAsync(user, await users.GenerateEmailConfirmationTokenAsync(user));
        var error = await service.RegisterAsync(TestKunde());
        Assert.StartsWith("E-postadressen er allerede i bruk.", error);
    }

// check if a failed mail does not fail the sign-up (the customer can sign up again for a new link)
    [Fact]
    public async Task RegisterAsync_MailFails_AccountIsStillCreated()
    {
        var (_, users, _) = CreateService();
        var service = new CustomerAccountService(users, new FailingEmailService(), "http://localhost:3000");
        var error = await service.RegisterAsync(TestKunde());
        Assert.Null(error);
        Assert.NotNull(await users.FindByEmailAsync("kari@test.example"));
    }

// check if a valid code confirms the email and a wrong one does not
    [Fact]
    public async Task ConfirmEmailAsync_ValidCode_ConfirmsEmail()
    {
        var (service, users, _) = CreateService();
        await service.RegisterAsync(TestKunde());
        var user = (await users.FindByEmailAsync("kari@test.example"))!;
        var code = await users.GenerateEmailConfirmationTokenAsync(user);
        var wrong = await service.ConfirmEmailAsync(new ConfirmEmailRequest(user.Id, "123456"));
        var ok = await service.ConfirmEmailAsync(new ConfirmEmailRequest(user.Id, code));
        Assert.False(wrong);
        Assert.True(ok);
        Assert.True((await users.FindByIdAsync(user.Id))!.EmailConfirmed);
    }
}