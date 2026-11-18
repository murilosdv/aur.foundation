using System;
using Aurore.Foundation.Core.Features.Attributes;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

/// <summary>
/// Enables the unique violation decorator for this feature.
/// </summary>
public sealed class WithUniqueViolationAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(UniqueViolationDecorator);
}
