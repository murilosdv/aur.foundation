using System;
using System.Diagnostics.CodeAnalysis;
using Aurore.Foundation.Core.Features.Decorators;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Enables the log line (outcome and duration) for this feature.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WithLoggingAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(LoggingDecorator);
}
