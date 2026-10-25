using besti2.Infrastructure.Services;

namespace besti2.Tests;

public class MailLimiterTests
{
    // check if the same address gets only one mail within 5 minutes (also with other letters/spaces)
    [Fact]
    public void TryReserve_SameAddressTwice_SecondIsRefused()
    {
        var limiter = new MailLimiter();
        Assert.True(limiter.TryReserve("kari@test.example"));
        Assert.False(limiter.TryReserve(" Kari@Test.example "));
        Assert.True(limiter.TryReserve("ola@test.example"));
    }

    // check if the address can get a mail again when the wait is over
    [Fact]
    public void TryReserve_AfterTheWait_IsAllowedAgain()
    {
        var limiter = new MailLimiter(cooldown: TimeSpan.Zero);
        Assert.True(limiter.TryReserve("kari@test.example"));
        Assert.True(limiter.TryReserve("kari@test.example"));
    }

    // check if no more mails go out when today's limit is used up, not even to new addresses
    [Fact]
    public void TryReserve_DailyLimitReached_RefusesEveryone()
    {
        var limiter = new MailLimiter(dailyLimit: 2);
        Assert.True(limiter.TryReserve("a@test.example"));
        Assert.True(limiter.TryReserve("b@test.example"));
        Assert.False(limiter.TryReserve("c@test.example"));
    }
}