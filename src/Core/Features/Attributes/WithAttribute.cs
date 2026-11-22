using System;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Enables a decorator for this feature: [With&lt;MyDecorator&gt;].
/// </summary>
/// <typeparam name="TDecorator">The decorator to enable.</typeparam>
[SuppressMessage("Major Code Smell", "S3993:Custom attributes should be marked with AttributeUsageAttribute", Justification = "The base class already sets the usage")]
public sealed class WithAttribute<TDecorator> : FeatureDecoratorAttribute
    where TDecorator : class, IFeatureDecorator
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TDecorator);
}
