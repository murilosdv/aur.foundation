using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Authentication.Configuration;
using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.MinimalApis.Configuration;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.AspNetCore.MinimalApis;

[Trait("Feature", "API versioning")]
public sealed class VersionedApiTests : IAsyncLifetime
{
    private TestApp _app = null!;

    public async ValueTask InitializeAsync()
    {
        var catalog = MinimalEndpointCatalog.Create([typeof(WidgetsResource), typeof(WidgetsV1), typeof(WidgetsV2), typeof(OpenResource), typeof(OpenV1), typeof(OpenV2)]);

        _app = await TestApp.StartAsync(
            builder => builder.Services
                .AddStandardApiVersioning(catalog)
                .AddStandardAuthConfiguration(new IdentityProviderOptions { AuthorityUrl = "https://idp.example.com/realms/test", Audience = "api" }, new ApiKeysOptions()),
            app =>
            {
                app.UseAuthentication().UseAuthorization();
                app.MapStandardMinimalEndpoints(catalog);
            });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "The group is served under the version in the URL, and the answer lists the supported and the deprecated versions")]
    public async Task VersionIsReadFromTheUrl()
    {
        // Arrange & Act
        using var v1 = await _app.Client.GetAsync("/v1/open/list", TestContext.Current.CancellationToken);
        using var v2 = await _app.Client.GetAsync("/v2/open/list", TestContext.Current.CancellationToken);
        using var v3 = await _app.Client.GetAsync("/v3/open/list", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, v1.StatusCode);
        Assert.Equal("1", v1.Headers.GetValues("api-supported-versions").Single());
        Assert.NotEqual(HttpStatusCode.OK, v3.StatusCode);
        Assert.Equal("2", v2.Headers.GetValues("api-deprecated-versions").Single());
    }

    [Fact(DisplayName = "A deprecated version carries the sunset date of the catalog")]
    public async Task DeprecatedVersionAdvertisesItsSunset()
    {
        // Arrange & Act
        using var response = await _app.Client.GetAsync("/v2/open/list", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("Wed, 01 Jan 2031", response.Headers.GetValues("Sunset").Single());
    }

    [Fact(DisplayName = "A group requires authorization unless it says otherwise")]
    public async Task GroupsFailClosed()
    {
        // Arrange & Act
        using var locked = await _app.Client.GetAsync("/v1/widgets/list", TestContext.Current.CancellationToken);
        using var open = await _app.Client.GetAsync("/v1/open/list", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, open.StatusCode);
    }

    [Fact(DisplayName = "The group name is the type name in slug case, and the API prefix is optional")]
    public void GroupNamesFollowTheTypeName()
    {
        // Arrange & Act
        var widgets = new WidgetsResource();

        // Assert
        Assert.Equal("widgets", widgets.Name);
        Assert.Equal(["Widgets"], widgets.Tags);
        Assert.False(widgets.PrefixWithApi);
        Assert.True(widgets.RequireAuthorization);
    }

    private sealed class WidgetsResource : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, new(2, true, new DateOnly(2031, 1, 1))];
    }

    private sealed class WidgetsV1 : MinimalEndpoint<WidgetsResource>
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("list", () => "widgets");
        }
    }

    private sealed class WidgetsV2 : MinimalEndpoint<WidgetsResource>
    {
        public override int Version => 2;

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("list", () => "widgets");
        }
    }

    private sealed class OpenResource : ResourceGroup
    {
        public override bool RequireAuthorization => false;

        public override MinimalEndpointVersion[] Versions => [1, new(2, true, new DateOnly(2031, 1, 1))];
    }

    private sealed class OpenV1 : MinimalEndpoint<OpenResource>
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("list", () => "open");
        }
    }

    private sealed class OpenV2 : MinimalEndpoint<OpenResource>
    {
        public override int Version => 2;

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("list", () => "open");
        }
    }
}
