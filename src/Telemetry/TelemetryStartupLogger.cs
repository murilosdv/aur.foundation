using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Telemetry.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.Telemetry;

/// <summary>
/// OTLP export failures are silent (the SDK only reports them through its own diagnostics), so the effective settings are
/// logged once at startup to make a wrong endpoint or protocol easy to spot.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed partial class TelemetryStartupLogger(
    ILogger<TelemetryStartupLogger> logger,
    TelemetryOptions telemetry)
    : IHostedService
{
    /// <param name="cancellationToken">A token to cancel the start.</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogExport(telemetry.Otlp.Protocol, telemetry.Otlp.Endpoint, telemetry.Tracing.SamplingRatio);

        return Task.CompletedTask;
    }

    /// <param name="cancellationToken">A token to cancel the stop.</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry is exported with {Protocol} to {Endpoint}; traces sampled at {SamplingRatio:P0}")]
    private partial void LogExport(string protocol, string endpoint, double samplingRatio);
}
