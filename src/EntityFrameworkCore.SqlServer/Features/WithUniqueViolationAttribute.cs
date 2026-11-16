using Aurore.Foundation.Core.Features.Attributes;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Features;

/// <summary>
/// Enables SQL Server unique-violation handling for a single feature.
/// </summary>
public sealed class WithUniqueViolationAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override System.Type DecoratorType => typeof(UniqueViolationDecorator);
}
