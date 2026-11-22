using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Results;

namespace Aurore.Foundation.Core.Features;

/// <summary>
/// The part of every feature that carries its pipeline. Derive from the generic Feature classes instead.
/// </summary>
public abstract class FeatureBase : IFeature
{
    private protected FeatureBase()
    {
    }

    /// <summary>
    /// Set by the registration; null when the feature is built by hand, for example in a test.
    /// </summary>
    internal FeaturePipeline? Pipeline { get; set; }

    internal void Attach(FeaturePipeline pipeline)
    {
        Pipeline = pipeline;
    }

    /// <summary>
    /// Runs the handler inside the decorators of the feature. Only called when the feature has a pipeline.
    /// </summary>
    internal ValueTask<Result> RunAsync(
        Type requestType,
        object request,
        FeatureDelegate handler,
        CancellationToken cancellationToken)
    {
        var context = new FeatureContext
        {
            FeatureType = GetType(),
            RequestType = requestType,
            Request = request,
            Services = Pipeline!.Services,
            CancellationToken = cancellationToken,
        };

        return Pipeline.RunAsync(context, handler);
    }
}
