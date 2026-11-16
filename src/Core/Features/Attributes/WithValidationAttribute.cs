using System;
using Aurore.Foundation.Core.Features.Decorators;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Enables the business validation (feature validators) for this feature.
/// </summary>
public sealed class WithValidationAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(ValidationDecorator);
}
