using System;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Disables a decorator for this feature, even when it is enabled for all: [Without&lt;LoggingDecorator&gt;].
/// </summary>
/// <typeparam name="TDecorator">The decorator to disable.</typeparam>
public sealed class WithoutAttribute<TDecorator> : FeatureDecoratorAttribute
    where TDecorator : class, IFeatureDecorator
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TDecorator);

    /// <inheritdoc />
    public override bool Removes => true;
}
