using System;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Enables a decorator for this feature: [With&lt;MyDecorator&gt;].
/// </summary>
/// <typeparam name="TDecorator">The decorator to enable.</typeparam>
public sealed class WithAttribute<TDecorator> : FeatureDecoratorAttribute
    where TDecorator : class, IFeatureDecorator
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TDecorator);
}
