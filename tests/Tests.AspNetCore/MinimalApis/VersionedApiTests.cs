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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.AspNetCore.MinimalApis;

[Trait("Feature", "API versioning")]
public sealed class VersionedApiTests : IAsyncLifetime
{
    private TestApp _app = null!;
    private Endpoint[] _endpoints = [];

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
                _endpoints = [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)];
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

    [Fact(DisplayName = "Endpoint metadata uses generated defaults when no properties or conventions are supplied")]
    public void MetadataUsesGeneratedDefaults()
    {
        // Arrange
        var declaration = new WidgetsV2();

        // Act
        var endpoint = Assert.Single(_endpoints, endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == declaration.RouteName);

        // Assert
        Assert.Equal(declaration.Summary, endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Null(endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>());
        Assert.Equal(declaration.RouteName, endpoint.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName);
    }

    [Fact(DisplayName = "Overridden endpoint properties replace generated metadata defaults")]
    public void MetadataUsesOverriddenProperties()
    {
        // Arrange
        var declaration = new WidgetsV1();

        // Act
        var endpoint = Assert.Single(_endpoints, endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == declaration.Name);

        // Assert
        Assert.Equal("Property summary", endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Equal("Property description", endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
        Assert.Equal(declaration.Name, endpoint.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName);
    }

    [Fact(DisplayName = "Explicit endpoint metadata overrides properties and the last convention wins")]
    public void LastExplicitMetadataWinsOverProperties()
    {
        // Arrange & Act
        var endpoint = Assert.Single(_endpoints, endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "ExplicitOpenV1");

        // Assert
        Assert.Equal("Last explicit summary", endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Equal("Last explicit description", endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
        Assert.Equal("ExplicitOpenV1", endpoint.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName);
    }

    [Fact(DisplayName = "Explicit names can resolve duplicate property names during mapping")]
    public async Task ExplicitNamesResolveDuplicateProperties()
    {
        // Arrange
        var catalog = MinimalEndpointCatalog.Create([typeof(NamingResource), typeof(FirstNamedEndpoint), typeof(SecondNamedEndpoint)]);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddStandardApiVersioning(catalog);
        await using var app = builder.Build();

        // Act
        app.MapStandardMinimalEndpoints(catalog);
        var names = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .ToArray();

        // Assert
        Assert.Contains("FirstExplicitName", names);
        Assert.Contains("SecondExplicitName", names);
        Assert.DoesNotContain("SharedPropertyName", names);
    }

    [Fact(DisplayName = "Duplicate final explicit endpoint names fail during mapping")]
    public async Task DuplicateExplicitNamesFailDuringMapping()
    {
        // Arrange
        var catalog = MinimalEndpointCatalog.Create([typeof(NamingResource), typeof(FirstNamedEndpoint), typeof(DuplicateNamedEndpoint)]);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddStandardApiVersioning(catalog);
        await using var app = builder.Build();

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => app.MapStandardMinimalEndpoints(catalog));

        // Assert
        Assert.Contains("share the route name 'FirstExplicitName'", error.Message);
    }

    [Fact(DisplayName = "Duplicate final property endpoint names still fail during mapping")]
    public async Task DuplicatePropertyNamesFailDuringMapping()
    {
        // Arrange
        var catalog = MinimalEndpointCatalog.Create([typeof(NamingResource), typeof(FirstPropertyEndpoint), typeof(SecondPropertyEndpoint)]);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddStandardApiVersioning(catalog);
        await using var app = builder.Build();

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => app.MapStandardMinimalEndpoints(catalog));

        // Assert
        Assert.Contains("share the route name 'SharedPropertyName'", error.Message);
    }

    [Fact(DisplayName = "Group metadata supplies defaults but explicit endpoint metadata still wins")]
    public async Task EndpointMetadataWinsOverGroupMetadata()
    {
        // Arrange
        var catalog = MinimalEndpointCatalog.Create([typeof(NamingResource), typeof(FirstNamedEndpoint)]);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddStandardApiVersioning(catalog);
        await using var app = builder.Build();

        // Act
        app.MapStandardMinimalEndpoints(catalog, group => group
            .WithName("GroupName")
            .WithSummary("Group summary")
            .WithDescription("Group description"));
        var endpoint = Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints));

        // Assert
        Assert.Equal("FirstExplicitName", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Equal("Group summary", endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Equal("Group description", endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
    }

    [Fact(DisplayName = "Late endpoint conventions override fallback metadata")]
    public async Task FinalConventionsOverrideFallbackMetadata()
    {
        // Arrange
        var catalog = MinimalEndpointCatalog.Create([typeof(NamingResource), typeof(FinalMetadataEndpoint)]);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddStandardApiVersioning(catalog);
        await using var app = builder.Build();

        // Act
        app.MapStandardMinimalEndpoints(catalog);
        var endpoint = Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints));

        // Assert
        Assert.Equal("FinalName", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Equal("FinalName", endpoint.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName);
        Assert.Equal("Final summary", endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Equal("Final description", endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
    }

    private sealed class FinalMetadataEndpoint : MinimalEndpoint<NamingResource>
    {
        public override string Name => "PropertyName";

        public override string Summary => "Property summary";

        public override string Description => "Property description";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            var route = builder.MapGet("final", () => "final");
            route.Finally(endpoint =>
            {
                endpoint.Metadata.Add(new EndpointNameMetadata("FinalName"));
                endpoint.Metadata.Add(new RouteNameMetadata("FinalName"));
                endpoint.Metadata.Add(new EndpointSummaryAttribute("Final summary"));
                endpoint.Metadata.Add(new EndpointDescriptionAttribute("Final description"));
            });
            return route;
        }
    }

    private sealed class NamingResource : ResourceGroup
    {
        public override bool RequireAuthorization => false;
    }

    private sealed class FirstNamedEndpoint : MinimalEndpoint<NamingResource>
    {
        public override string Name => "SharedPropertyName";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("first", () => "first").WithName("FirstExplicitName");
        }
    }

    private sealed class SecondNamedEndpoint : MinimalEndpoint<NamingResource>
    {
        public override string Name => "SharedPropertyName";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("second", () => "second").WithName("SecondExplicitName");
        }
    }

    private sealed class DuplicateNamedEndpoint : MinimalEndpoint<NamingResource>
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            var route = builder.MapGet("duplicate", () => "duplicate").WithName("InitiallyUniqueName");
            route.Finally(endpoint =>
            {
                endpoint.Metadata.Add(new EndpointNameMetadata("FirstExplicitName"));
                endpoint.Metadata.Add(new RouteNameMetadata("FirstExplicitName"));
            });
            return route;
        }
    }

    private sealed class FirstPropertyEndpoint : MinimalEndpoint<NamingResource>
    {
        public override string Name => "SharedPropertyName";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("first", () => "first");
        }
    }

    private sealed class SecondPropertyEndpoint : MinimalEndpoint<NamingResource>
    {
        public override string Name => "SharedPropertyName";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("second", () => "second");
        }
    }

    private sealed class WidgetsResource : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, new(2, true, new DateOnly(2031, 1, 1))];
    }

    private sealed class WidgetsV1 : MinimalEndpoint<WidgetsResource>
    {
        public override string Name => "PropertyWidgetsV1";

        public override string Summary => "Property summary";

        public override string Description => "Property description";

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
            return builder.MapGet("list", Handle);

            static string Handle() => "widgets";
        }
    }

    private sealed class OpenResource : ResourceGroup
    {
        public override bool RequireAuthorization => false;

        public override MinimalEndpointVersion[] Versions => [1, new(2, true, new DateOnly(2031, 1, 1))];
    }

    private sealed class OpenV1 : MinimalEndpoint<OpenResource>
    {
        public override string Name => "PropertyOpenV1";

        public override string Summary => "Property summary";

        public override string Description => "Property description";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("list", () => "open")
                .WithName("FirstExplicitOpenV1")
                .WithSummary("First explicit summary")
                .WithDescription("First explicit description")
                .WithName("ExplicitOpenV1")
                .WithSummary("Last explicit summary")
                .WithDescription("Last explicit description");
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
