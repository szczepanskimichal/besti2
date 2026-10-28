using System.Net;
using besti2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace besti2.Tests;

// The whole API in memory, called through HTTP like the web does: routes, login, roles and limits.
// Its own in-memory database and no background jobs (reminders, mail queue) in the tests.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // SQL Server -> in-memory database
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));

            services.RemoveAll<IHostedService>();
            services.AddSingleton<IStartupFilter, LocalCallerFilter>();
        });
    }

    // A test request has no IP; this one looks like it comes from the web server on the same machine,
    // so X-Forwarded-For is trusted like on the real server
    private class LocalCallerFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
