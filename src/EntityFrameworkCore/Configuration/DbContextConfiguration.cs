using System;
using Aurore.Foundation.EntityFrameworkCore.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aurore.Foundation.EntityFrameworkCore.Configuration;

/// <summary>
/// Registers a context with provider-independent defaults.
/// </summary>
public static class DbContextConfiguration
{
    /// <summary>
    /// Registers a provider-configured context with no-tracking queries, soft delete and timestamps.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Selects the provider and configures its options.</param>
    public static IServiceCollection AddStandardDbContext<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
        where TContext : DbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<SoftDeleteInterceptor>();
        services.TryAddSingleton<TimestampInterceptor>();

        return services.AddDbContext<TContext>((provider, builder) =>
        {
            builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .AddInterceptors(provider.GetRequiredService<SoftDeleteInterceptor>(), provider.GetRequiredService<TimestampInterceptor>());
            configure(builder);
        });
    }
}
