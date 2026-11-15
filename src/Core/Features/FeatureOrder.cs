using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Features;

/// <summary>
/// Where the decorators of the foundation sit in the pipeline of a feature: lower runs first (outermost). Inherit from it to
/// name the places an application adds, such as <c>Audit = Validation + 50</c>.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class FeatureOrder
{
    /// <summary>
    /// Telemetry, outermost, so it measures everything else.
    /// </summary>
    public const int Telemetry = 100;

    /// <summary>
    /// Logging of the outcome.
    /// </summary>
    public const int Logging = 200;

    /// <summary>
    /// Validation of the request, before the feature runs.
    /// </summary>
    public const int Validation = 300;

    /// <summary>
    /// The default of a custom decorator: right before the feature.
    /// </summary>
    public const int Default = 1000;

    /// <summary>
    /// Lets an application inherit the constants; there is nothing to create.
    /// </summary>
    protected FeatureOrder()
    {
    }
}
