using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Authentication.Configuration;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.RequestPipeline;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Foundation.Tests.AspNetCore.RequestPipeline;

/// <summary>
/// The pipeline registered and used the way an application does, on an in-memory server.
/// </summary>
[Trait("Feature", "Request pipeline")]
public sealed class RequestPipelineTests : IAsyncLifetime
{
    private TestApp _app = null!;

    public async ValueTask InitializeAsync()
    {
        var options = new RequestPipelineOptions();
        var identity = new IdentityProviderOptions { AuthorityUrl = "https://idp.example.com/realms/test", Audience = "api" };

        _app = await TestApp.StartAsync(
            builder =>
            {
                builder.AddStandardRequestPipeline(options);
                builder.Services.AddStandardAuthConfiguration(identity, new ApiKeysOptions());
            },
            app =>
            {
                app.UseStandardRequestPipeline(options).UseAuthentication().UseAuthorization().UseStandardEndpointPolicies();

                app.MapGet("/correlation", (RequestContext context) => new { context.TraceId });
                app.MapGet("/boom", IResult () => throw new System.InvalidOperationException("Secret detail."));
                app.MapGet("/secure", () => "secure").RequireAuthorization();
            });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "The Correlation-Id the caller sends is used and echoed back on the response")]
    public async Task CorrelationIdIsEchoed()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/correlation");
        request.Headers.Add("Correlation-Id", "my-correlation-id");

        // Act
        using var response = await _app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("my-correlation-id", response.Headers.GetValues("Correlation-Id").Single());
    }

    [Fact(DisplayName = "Without a header the correlation id is the trace id, and the response says so")]
    public async Task CorrelationIdDefaultsToTheTraceId()
    {
        // Arrange & Act
        using var response = await _app.Client.GetAsync("/correlation", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var correlationId = response.Headers.GetValues("Correlation-Id").Single();
        Assert.Equal(32, correlationId.Length);
        Assert.Contains($"\"traceId\":\"{correlationId}\"", body);
    }

    [Fact(DisplayName = "An unhandled exception returns a 500 problem that still carries the caller's correlation id and hides the exception")]
    public async Task UnhandledExceptionKeepsTheCorrelationId()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/boom");
        request.Headers.Add("Correlation-Id", "my-correlation-id");

        // Act
        using var response = await _app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("my-correlation-id", response.Headers.GetValues("Correlation-Id").Single());
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("Secret detail", body);
    }

    [Fact(DisplayName = "An unknown route returns a 404 with a problem body instead of an empty response")]
    public async Task UnknownRouteReturnsAProblem()
    {
        // Arrange & Act
        using var response = await _app.Client.GetAsync("/nothing-here", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact(DisplayName = "A protected endpoint called without credentials returns a 401 with a problem body")]
    public async Task UnauthenticatedCallReturnsAProblem()
    {
        // Arrange & Act
        using var response = await _app.Client.GetAsync("/secure", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
