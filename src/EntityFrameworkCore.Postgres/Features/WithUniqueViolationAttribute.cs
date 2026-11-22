using System;
using System.Diagnostics.CodeAnalysis;
using Aurore.Foundation.Core.Features.Attributes;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

/// <summary>
/// Enables the unique violation decorator for this feature.
/// </summary>
[SuppressMessage("Major Code Smell", "S3993:Custom attributes should be marked with AttributeUsageAttribute", Justification = "The base class already sets the usage")]
public sealed class WithUniqueViolationAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(UniqueViolationDecorator);
}
