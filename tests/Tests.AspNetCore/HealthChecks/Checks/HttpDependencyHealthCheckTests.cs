using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.HealthChecks.Checks;
using Aurore.Foundation.Tests.AspNetCore.Support;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aurore.Foundation.Tests.AspNetCore.HealthChecks.Checks;

[Trait("Feature", "HTTP dependency health check")]
public sealed class HttpDependencyHealthCheckTests
{
    public static TheoryData<HttpStatusCode> SuccessStatuses => [HttpStatusCode.OK, HttpStatusCode.NoContent];

    public static TheoryData<HttpStatusCode, HealthStatus> FailureStatuses => new()
    {
        { HttpStatusCode.ServiceUnavailable, HealthStatus.Unhealthy },
        { HttpStatusCode.NotFound, HealthStatus.Unhealthy },
        { HttpStatusCode.ServiceUnavailable, HealthStatus.Degraded },
        { HttpStatusCode.InternalServerError, HealthStatus.Degraded },
    };

    [Theory(DisplayName = "A dependency that answers with a success status is healthy")]
    [MemberData(nameof(SuccessStatuses))]
    public async Task SuccessStatusIsHealthy(HttpStatusCode status)
    {
        // Arrange
        var check = Check(_ => new HttpResponseMessage(status));

        // Act
        var result = await check.CheckHealthAsync(Context(check, HealthStatus.Unhealthy), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Theory(DisplayName = "Any other status gives the failure status of the check: unhealthy when critical, degraded when not")]
    [MemberData(nameof(FailureStatuses))]
    public async Task OtherStatusGivesTheFailureStatus(HttpStatusCode status, HealthStatus failureStatus)
    {
        // Arrange
        var check = Check(_ => new HttpResponseMessage(status));

        // Act
        var result = await check.CheckHealthAsync(Context(check, failureStatus), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(failureStatus, result.Status);
        Assert.Contains(((int)status).ToString(), result.Description);
    }

    [Theory(DisplayName = "A dependency that cannot be reached gives the failure status of the check, without leaking the error to the caller")]
    [InlineData(HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded)]
    public async Task UnreachableGivesTheFailureStatus(HealthStatus failureStatus)
    {
        // Arrange
        var check = Check(_ => throw new HttpRequestException("connection refused to 10.0.0.7"));

        // Act
        var result = await check.CheckHealthAsync(Context(check, failureStatus), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(failureStatus, result.Status);
        Assert.Equal("Dependency could not be reached.", result.Description);
    }

    [Fact(DisplayName = "The check uses the HTTP client named after the check")]
    public void ClientIsNamedAfterTheCheck()
    {
        // Act
        var name = HttpDependencyHealthCheck.GetClientName("orders");

        // Assert
        Assert.Equal("health-check-orders", name);
    }

    private static HttpDependencyHealthCheck Check(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var factory = new StubClientFactory(new StubHandler(respond), new Uri("http://orders.test/health/ready"));

        return new HttpDependencyHealthCheck(NullLogger<HttpDependencyHealthCheck>.Instance, factory);
    }

    private static HealthCheckContext Context(IHealthCheck check, HealthStatus failureStatus)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("orders", check, failureStatus, tags: null),
        };
    }
}
