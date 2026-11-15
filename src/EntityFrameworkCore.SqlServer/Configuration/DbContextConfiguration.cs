using System;
using Aurore.Foundation.EntityFrameworkCore.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Constants;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Configuration;

/// <summary>
/// Registers SQL Server contexts with the shared Foundation defaults.
/// </summary>
public static class DbContextConfiguration
{
    /// <summary>
    /// Registers SQL Server, no-tracking queries, soft delete and timestamps. Apply shared model conventions in OnModelCreating.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The connection settings.</param>
    /// <param name="configure">Additional context configuration.</param>
    public static IServiceCollection AddStandardDbContext<TContext>(this IServiceCollection services, SqlServerOptions options, Action<DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        var connectionString = options.ToConnectionString();
        return services.AddStandardDbContext<TContext>(builder =>
        {
            builder.UseSqlServer(connectionString, provider => provider.MigrationsHistoryTable("ef_migrations_history", DatabaseDefaults.Schema));
            configure?.Invoke(builder);
        });
    }
}
