using System;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Disables a decorator for this feature, even when it is enabled for all: [Without&lt;LoggingDecorator&gt;].
/// </summary>
/// <typeparam name="TDecorator">The decorator to disable.</typeparam>
[SuppressMessage("Major Code Smell", "S3993:Custom attributes should be marked with AttributeUsageAttribute", Justification = "The base class already sets the usage")]
public sealed class WithoutAttribute<TDecorator> : FeatureDecoratorAttribute
    where TDecorator : class, IFeatureDecorator
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TDecorator);

    /// <inheritdoc />
    public override bool Removes => true;
}
