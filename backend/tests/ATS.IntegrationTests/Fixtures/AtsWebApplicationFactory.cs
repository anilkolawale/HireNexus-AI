using ATS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATS.IntegrationTests.Fixtures;

// Swaps the SQL Server DbContext for EF Core's InMemory provider so integration tests run
// without a real database — each test class gets an isolated, uniquely-named in-memory DB.
public class AtsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AtsDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AtsDbContext>(options =>
                options.UseInMemoryDatabase($"ats-integration-tests-{Guid.NewGuid()}"));
        });

        builder.UseEnvironment("Testing");
    }
}
