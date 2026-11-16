using System;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.EntityFrameworkCore.Features;

/// <summary>
/// Registers constraint-specific errors for providers that expose a structured constraint name.
/// </summary>
public static class UniqueViolationConfiguration
{
    /// <summary>
    /// Registers the mapping consumed by supported provider-specific unique-violation decorators.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Maps database constraint names to application errors.</param>
    public static IServiceCollection AddUniqueViolationErrors(
        this IServiceCollection services,
        Action<UniqueViolationMap> configure)
    {
        var map = new UniqueViolationMap();
        configure(map);
        return services.AddSingleton(map);
    }
}
