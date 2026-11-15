using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Telemetry.Configuration;
using Aurore.Foundation.Telemetry.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Aurore.Foundation.Tests.Telemetry.Configuration;

[Trait("Feature", "Telemetry")]
public sealed class TelemetryRegistrationTests
{
    private static readonly AppInformationOptions _appInfo = new()
    {
        Component = "orders-service",
        Domain = "sales",
        Description = "Takes orders.",
        Boundary = "orders",
        Maintainer = new AppMaintainerOptions("Orders Team", "orders@example.com")
    };

    public static TheoryData<string, string> Protocols => new()
    {
        { Environments.Development, "Grpc" },
        { Environments.Production, "HttpProtobuf" },
    };

    [Theory(DisplayName = "The traces, the metrics and the startup log are registered in any environment and protocol")]
    [MemberData(nameof(Protocols))]
    public void TelemetryIsRegistered(string environment, string protocol)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        var telemetry = new TelemetryOptions { Otlp = new TelemetryOtlpOptions { Endpoint = "http://127.0.0.1:4318", Protocol = protocol } };

        // Act
        builder.AddStandardTelemetry(_appInfo, telemetry);
        using var app = builder.Build();

        // Assert
        Assert.NotNull(app.Services.GetService<TracerProvider>());
        Assert.NotNull(app.Services.GetService<MeterProvider>());
        Assert.Contains(app.Services.GetServices<IHostedService>(), x => x.GetType().Name == "TelemetryStartupLogger");
    }
}
