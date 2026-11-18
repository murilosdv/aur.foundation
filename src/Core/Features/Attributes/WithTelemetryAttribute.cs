using System;
using System.Diagnostics.CodeAnalysis;
using Aurore.Foundation.Core.Features.Decorators;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Enables the telemetry span for this feature.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WithTelemetryAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TelemetryDecorator);
}
