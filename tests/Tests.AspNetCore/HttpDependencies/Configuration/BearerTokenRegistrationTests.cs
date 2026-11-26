using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.HttpDependencies.Configuration;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Aurore.Foundation.Tests.AspNetCore.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.AspNetCore.HttpDependencies.Configuration;

[Trait("Feature", "HTTP dependency registration")]
public sealed class BearerTokenRegistrationTests
{
    private static readonly HttpDependencyOptions _dependency = new()
    {
        BaseUrl = "https://inventory.example",
        PropagateCorrelationId = false,
    };

    [Fact(DisplayName = "A client registered with a bearer token sends it in the Authorization header")]
    public async Task RegisteredClientSendsTheToken()
    {
        // Arrange
        var upstream = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var services = new ServiceCollection();
        services.AddStandardHttpDependency("inventory", _dependency)
            .AddStandardBearerToken("Bearer static-token")
            .ConfigurePrimaryHttpMessageHandler(() => upstream);
        using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("inventory");

        // Act
        await client.GetAsync(new Uri("items", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Bearer static-token"], upstream.Requests[0].Headers.GetValues("Authorization"));
    }

    [Theory(DisplayName = "Registering without a token fails, naming the dependency")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer ")]
    public void MissingTokenFails(string? token)
    {
        // Arrange
        var builder = new ServiceCollection().AddStandardHttpDependency("inventory", _dependency);

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => builder.AddStandardBearerToken(token));

        // Assert
        Assert.Contains("'inventory'", error.Message, StringComparison.Ordinal);
    }
}
