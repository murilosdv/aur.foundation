using Aurore.Foundation.Core.Features.Configuration;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Features;

/// <summary>
/// Opt-in SQL Server unique-constraint conflict handling.
/// </summary>
public static class UniqueViolationConfiguration
{
    /// <summary>
    /// Enables the SQL Server unique-violation decorator for registered features.
    /// </summary>
    public static FeaturesBuilder WithUniqueViolation(this FeaturesBuilder builder)
    {
        return builder.With<UniqueViolationDecorator>();
    }
}
