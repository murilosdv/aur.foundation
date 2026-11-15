using System.Threading.Tasks;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.HealthChecks;
using Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.HealthChecks;

[Trait("Feature", "SqlServer health check")]
[Trait("Category", "Integration")]
public sealed class SqlServerHealthCheckTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Fact(DisplayName = "A reachable database is healthy")]
    public async Task ReachableDatabaseIsHealthy()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSqlServerHealthCheck(database.Options)
            .BuildServiceProvider();

        // Act
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Contains("sqlserver", report.Entries.Keys);
    }

    [Fact(DisplayName = "An unreachable database is unhealthy")]
    public async Task UnreachableDatabaseIsUnhealthy()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSqlServerHealthCheck(SqlServerFixture.Unreachable())
            .BuildServiceProvider();

        // Act
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }
}
