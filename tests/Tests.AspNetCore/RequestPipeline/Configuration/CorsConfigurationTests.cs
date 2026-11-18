using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Aurore.Foundation.Tests.AspNetCore.RequestPipeline.Configuration;

[Trait("Feature", "CORS")]
public sealed class CorsConfigurationTests
{
    private const string Origin = "https://app.example.com";

    public static TheoryData<string, CorsPolicyOptions, string> RefusedPolicies => new()
    {
        { Environments.Development, new() { AllowedOrigins = ["*"], AllowCredentials = true }, "credentials with a wildcard" },
        { Environments.Production, new() { AllowedOrigins = ["*"] }, "any origin" },
        { Environments.Development, new() { AllowedOrigins = ["https://app.example.com/path"] }, "invalid origin" },
        { Environments.Development, new() { AllowedOrigins = ["https://app.example.com/"] }, "invalid origin" },
        { Environments.Development, new() { AllowedOrigins = ["not a url"] }, "invalid origin" },
        { Environments.Production, new() { AllowedOrigins = ["http://localhost:3000"] }, "local origin" },
        { Environments.Production, new() { AllowedOrigins = ["https://web.localhost"] }, "local origin" },
    };

    [Theory(DisplayName = "A policy that is unsafe or malformed stops the startup, naming the reason")]
    [MemberData(nameof(RefusedPolicies))]
    public void UnsafePolicyIsRefused(string environment, CorsPolicyOptions policy, string reason)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });

        // Act
        var act = () => builder.AddStandardCorsPolicies(new Dictionary<string, CorsPolicyOptions> { ["web"] = policy });

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains(reason, exception.Message);
    }

    [Fact(DisplayName = "Two policies both named default are refused")]
    public void TwoDefaultPoliciesAreRefused()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var policies = new Dictionary<string, CorsPolicyOptions>
        {
            ["default"] = new() { AllowedOrigins = [Origin] },
            ["DEFAULT"] = new() { AllowedOrigins = [Origin] },
        };

        // Act
        var act = () => builder.AddStandardCorsPolicies(policies);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact(DisplayName = "Without policies nothing is registered and the answer carries no CORS header")]
    public async Task WithoutPoliciesNothingIsAllowed()
    {
        // Arrange
        await using var app = await StartAsync(new Dictionary<string, CorsPolicyOptions>());

        // Act
        using var response = await GetFromAsync(app, Origin);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact(DisplayName = "The default policy lets its origin read the response, exposing the correlation id, and refuses another origin")]
    public async Task DefaultPolicyAllowsItsOriginOnly()
    {
        // Arrange
        var policy = new CorsPolicyOptions { AllowedOrigins = [Origin], AllowedMethods = ["GET"] };
        await using var app = await StartAsync(new Dictionary<string, CorsPolicyOptions> { ["default"] = policy });

        // Act
        using var allowed = await GetFromAsync(app, Origin);
        using var refused = await GetFromAsync(app, "https://evil.example.com");

        // Assert
        Assert.Equal(Origin, allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("Correlation-Id", allowed.Headers.GetValues("Access-Control-Expose-Headers").Single());
        Assert.False(refused.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact(DisplayName = "A preflight is answered with the methods, the headers (the defaults and the configured ones), the credentials and the max age")]
    public async Task PreflightIsAnsweredFromThePolicy()
    {
        // Arrange
        var policy = new CorsPolicyOptions
        {
            AllowedOrigins = [Origin],
            AllowedMethods = ["GET", "POST"],
            AllowedHeaders = ["X-Custom", "authorization"],
            AllowCredentials = true,
            PreflightMaxAgeSeconds = 600,
        };
        await using var app = await StartAsync(new Dictionary<string, CorsPolicyOptions> { ["default"] = policy });
        using var request = new HttpRequestMessage(HttpMethod.Options, "/");
        request.Headers.Add("Origin", Origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "x-custom, idempotency-key");

        // Act
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Equal("600", response.Headers.GetValues("Access-Control-Max-Age").Single());
        var headers = response.Headers.GetValues("Access-Control-Allow-Headers").Single();
        Assert.Contains("X-Custom", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Idempotency-Key", headers, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "A named policy only applies where an endpoint asks for it")]
    public async Task NamedPolicyAppliesToTheEndpointThatAsks()
    {
        // Arrange
        var policy = new CorsPolicyOptions { AllowedOrigins = [Origin], AllowedMethods = ["GET"] };
        await using var app = await StartAsync(
            new Dictionary<string, CorsPolicyOptions> { ["partners"] = policy },
            map: web =>
            {
                web.MapGet("/partners", () => "ok").RequireCors("partners");
            });

        // Act
        using var withPolicy = await GetFromAsync(app, Origin, "/partners");
        using var without = await GetFromAsync(app, Origin);

        // Assert
        Assert.True(withPolicy.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(without.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static Task<TestApp> StartAsync(
        IReadOnlyDictionary<string, CorsPolicyOptions> policies,
        Action<WebApplication>? map = null)
    {
        return TestApp.StartAsync(
            builder => builder.AddStandardCorsPolicies(policies),
            app =>
            {
                app.UseRouting();
                app.UseStandardCors(policies);
                app.MapGet("/", () => "ok");
                map?.Invoke(app);
            });
    }

    private static Task<HttpResponseMessage> GetFromAsync(TestApp app, string origin, string path = "/")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Origin", origin);

        return app.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
