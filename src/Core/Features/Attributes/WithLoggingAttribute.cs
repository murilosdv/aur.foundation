using System;
using System.Diagnostics.CodeAnalysis;
using Aurore.Foundation.Core.Features.Decorators;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Enables the log line (outcome and duration) for this feature.
/// </summary>
[ExcludeFromCodeCoverage]
[SuppressMessage("Major Code Smell", "S3993:Custom attributes should be marked with AttributeUsageAttribute", Justification = "The base class already sets the usage")]
public sealed class WithLoggingAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(LoggingDecorator);
}
