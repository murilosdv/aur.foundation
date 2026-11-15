using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.HealthChecks.Constants;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.HealthChecks;

/// <summary>
/// SQL Server connectivity checks independent of a DbContext.
/// </summary>
public static class SqlServerHealthCheckConfiguration
{
    /// <summary>
    /// Registers a SELECT 1 readiness check with its own pooled SQL connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The SQL Server connection settings.</param>
    public static IServiceCollection AddSqlServerHealthCheck(this IServiceCollection services, SqlServerOptions options)
    {
        var connectionString = options.ToConnectionString();
        services.AddHealthChecks().Add(new HealthCheckRegistration(
            "sqlserver", _ => new SqlServerHealthCheck(connectionString), HealthStatus.Unhealthy,
            [HealthCheckProperties.Tags.Readiness, HealthCheckProperties.Tags.TypeDatabase, HealthCheckProperties.Tags.Critical],
            TimeSpan.FromSeconds(HealthCheckProperties.Configuration.DefaultTimeoutSeconds)));
        return services;
    }
}

internal sealed class SqlServerHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is 1 ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("SQL Server is unavailable.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Could not reach SQL Server.");
        }
    }
}
