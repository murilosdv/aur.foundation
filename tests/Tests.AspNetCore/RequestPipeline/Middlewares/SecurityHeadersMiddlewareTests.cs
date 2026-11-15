using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Aurore.Foundation.Tests.AspNetCore.Support;

namespace Aurore.Foundation.Tests.AspNetCore.RequestPipeline.Middlewares;

[Trait("Feature", "Security headers")]
public sealed class SecurityHeadersMiddlewareTests
{
    [Fact(DisplayName = "Every response gets the headers against sniffing, framing and the referrer")]
    public async Task ResponseGetsTheFixedHeaders()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        var response = ResponseStartFeature.Install(http);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, new SecurityHeadersOptions());

        // Act
        await middleware.InvokeAsync(http);
        await response.StartAsync();

        // Assert
        Assert.Equal("nosniff", http.Response.Headers.XContentTypeOptions);
        Assert.Equal("DENY", http.Response.Headers.XFrameOptions);
        Assert.Equal("no-referrer", http.Response.Headers["Referrer-Policy"]);
    }

    [Fact(DisplayName = "The content security policy is only sent when one is configured")]
    public async Task PolicyIsSentOnlyWhenConfigured()
    {
        // Arrange
        var without = HttpContextFactory.Create();
        var withoutResponse = ResponseStartFeature.Install(without);
        var with = HttpContextFactory.Create();
        var withResponse = ResponseStartFeature.Install(with);

        // Act
        await new SecurityHeadersMiddleware(_ => Task.CompletedTask, new SecurityHeadersOptions { ContentSecurityPolicy = null }).InvokeAsync(without);
        await new SecurityHeadersMiddleware(_ => Task.CompletedTask, new SecurityHeadersOptions { ContentSecurityPolicy = "default-src 'none'" }).InvokeAsync(with);
        await withoutResponse.StartAsync();
        await withResponse.StartAsync();

        // Assert
        Assert.False(without.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("default-src 'none'", with.Response.Headers.ContentSecurityPolicy);
    }

    [Fact(DisplayName = "The request carries on to the next component")]
    public async Task RequestCarriesOn()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        var called = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            called = true;

            return Task.CompletedTask;
        }, new SecurityHeadersOptions());

        // Act
        await middleware.InvokeAsync(http);

        // Assert
        Assert.True(called);
    }
}
