using System;
using System.Diagnostics.CodeAnalysis;
using Aurore.Foundation.Core.Features.Decorators;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Enables the business validation (feature validators) for this feature.
/// </summary>
[SuppressMessage("Major Code Smell", "S3993:Custom attributes should be marked with AttributeUsageAttribute", Justification = "The base class already sets the usage")]
public sealed class WithValidationAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(ValidationDecorator);
}
