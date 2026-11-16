using Aurore.Foundation.Core.Features.Configuration;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

/// <summary>
/// Turns on PostgreSQL unique-violation handling.
/// </summary>
public static class UniqueViolationConfiguration
{
    /// <summary>
    /// Enables the <see cref="UniqueViolationDecorator"/> for every feature of the registration.
    /// </summary>
    public static FeaturesBuilder WithUniqueViolation(this FeaturesBuilder builder)
    {
        return builder.With<UniqueViolationDecorator>();
    }

}
