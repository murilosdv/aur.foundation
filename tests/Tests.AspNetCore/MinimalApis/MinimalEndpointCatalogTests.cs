using System;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.MinimalApis.Configuration;
using Aurore.Foundation.Tests.Shared.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.AspNetCore.MinimalApis;

[Trait("Feature", "Minimal endpoints")]
public sealed class MinimalEndpointCatalogTests
{
    [Fact(DisplayName = "Versions are distinct and ascending across groups")]
    public void VersionsAreDistinctAndAscendingAcrossGroups()
    {
        // Arrange
        Type[] types = [typeof(OrdersGroup), typeof(OrdersV1), typeof(OrdersV2), typeof(BillingGroup), typeof(BillingV2), typeof(BillingV3)];

        // Act
        var catalog = Create(types);

        // Assert
        Assert.Equal([1, 2, 3], catalog.Versions.Select(x => x.Number));
    }

    [Fact(DisplayName = "A deprecated version keeps its sunset date")]
    public void DeprecatedVersionKeepsItsSunsetDate()
    {
        // Arrange
        Type[] types = [typeof(DeprecatingGroup), typeof(DeprecatingV1), typeof(DeprecatingV2)];

        // Act
        var catalog = Create(types);

        // Assert
        Assert.Equal(new MinimalEndpointVersion(1), catalog.Versions[0]);
        Assert.Equal(new MinimalEndpointVersion(2, true, new DateOnly(2030, 1, 1)), catalog.Versions[1]);
    }

    [Fact(DisplayName = "Throws when an endpoint targets a version its group does not declare")]
    public void ThrowsWhenEndpointTargetsUndeclaredVersion()
    {
        // Arrange
        Type[] types = [typeof(UndeclaredGroup), typeof(UndeclaredV1), typeof(UndeclaredV2)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("targeting version 2", error.Message);
    }

    [Fact(DisplayName = "Throws when a declared version has no endpoint")]
    public void ThrowsWhenDeclaredVersionHasNoEndpoint()
    {
        // Arrange
        Type[] types = [typeof(NoEndpointGroup), typeof(NoEndpointV1)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("[2] without any endpoint", error.Message);
    }

    [Fact(DisplayName = "Throws when a group declares the same version twice")]
    public void ThrowsWhenVersionsAreDuplicated()
    {
        // Arrange
        Type[] types = [typeof(DuplicatedGroup), typeof(DuplicatedV1)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("duplicate versions", error.Message);
    }

    [Fact(DisplayName = "Throws when a sunset date is set on a version that is not deprecated")]
    public void ThrowsWhenSunsetIsSetOnAnActiveVersion()
    {
        // Arrange
        Type[] types = [typeof(SunsetOnActiveGroup), typeof(SunsetOnActiveV1)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("not deprecated", error.Message);
    }

    [Fact(DisplayName = "Throws when two groups share the same name")]
    public void ThrowsWhenGroupNamesAreNotUnique()
    {
        // Arrange
        Type[] types = [typeof(FirstSameNameGroup), typeof(SecondSameNameGroup)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("share the name 'same'", error.Message);
    }

    [Fact(DisplayName = "Throws when groups disagree on whether a version is deprecated")]
    public void ThrowsWhenGroupsDisagreeOnDeprecation()
    {
        // Arrange
        Type[] types = [typeof(OrdersGroup), typeof(OrdersV1), typeof(OrdersV2), typeof(DeprecatingGroup), typeof(DeprecatingV1), typeof(DeprecatingV2)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("Version 2", error.Message);
        Assert.Contains("deprecation", error.Message);
    }

    [Fact(DisplayName = "Throws when groups disagree on the sunset date of a version")]
    public void ThrowsWhenGroupsDisagreeOnSunsetDate()
    {
        // Arrange
        Type[] types = [typeof(DeprecatingGroup), typeof(DeprecatingV1), typeof(DeprecatingV2), typeof(LaterSunsetGroup), typeof(LaterSunsetV2)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("Version 2", error.Message);
        Assert.Contains("sunset dates", error.Message);
    }

    [Fact(DisplayName = "Throws when two endpoints share the same route name")]
    public void ThrowsWhenRouteNamesAreNotUnique()
    {
        // Arrange
        Type[] types = [typeof(SameRouteNameGroup), typeof(SameRouteNameFirst), typeof(SameRouteNameSecond)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("share the route name 'same-route'", error.Message);
    }

    [Fact(DisplayName = "Nothing set: the name and the summary are derived from the class")]
    public async Task NothingSetUsesTheDerivedValues()
    {
        // Arrange & Act
        var (name, summary, description) = await MappedMetadataAsync("derived", typeof(NamesGroup), typeof(DerivedNamesEndpoint));

        // Assert
        Assert.Equal("DerivedNames", name);
        Assert.Equal("Derived Names", summary);
        Assert.Null(description);
    }

    [Fact(DisplayName = "A property override replaces the derived values")]
    public async Task PropertyOverrideReplacesTheDerivedValues()
    {
        // Arrange & Act
        var (name, summary, description) = await MappedMetadataAsync("overridden", typeof(NamesGroup), typeof(OverriddenNamesEndpoint));

        // Assert
        Assert.Equal("CustomName", name);
        Assert.Equal("Custom summary", summary);
        Assert.Equal("Custom description", description);
    }

    [Fact(DisplayName = "WithName, WithSummary and WithDescription inside Map win over the derived values")]
    public async Task FluentCallsWinOverTheDerivedValues()
    {
        // Arrange & Act
        var (name, summary, description) = await MappedMetadataAsync("fluent", typeof(NamesGroup), typeof(FluentNamesEndpoint));

        // Assert
        Assert.Equal("FluentName", name);
        Assert.Equal("Fluent summary", summary);
        Assert.Equal("Fluent description", description);
    }

    [Fact(DisplayName = "WithName, WithSummary and WithDescription inside Map win over the property override")]
    public async Task FluentCallsWinOverThePropertyOverride()
    {
        // Arrange & Act
        var (name, summary, description) = await MappedMetadataAsync("fluent-over-override", typeof(NamesGroup), typeof(FluentOverOverrideNamesEndpoint));

        // Assert
        Assert.Equal("WinnerName", name);
        Assert.Equal("Winner summary", summary);
        Assert.Equal("Winner description", description);
    }

    [Fact(DisplayName = "Mapping fails when WithName gives two endpoints the same name")]
    public async Task ThrowsAtStartWhenWithNameIsNotUnique()
    {
        // Arrange
        Type[] types = [typeof(NamesGroup), typeof(FirstDuplicateNameEndpoint), typeof(SecondDuplicateNameEndpoint)];

        // Act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => MappedMetadataAsync("first-duplicate", types));

        // Assert
        Assert.Contains("share the route name 'Duplicated'", error.Message);
    }

    private static MinimalEndpointCatalog Create(params Type[] types)
    {
        return MinimalEndpointCatalog.Create(types);
    }

    private static async Task<(string? Name, string? Summary, string? Description)> MappedMetadataAsync(string route, params Type[] types)
    {
        var catalog = Create(types);
        EndpointDataSource? endpoints = null;

        await using var app = await TestApp.StartAsync(
            builder => builder.Services.AddStandardApiVersioning(catalog),
            app =>
            {
                app.MapStandardMinimalEndpoints(catalog);
                endpoints = app.Services.GetRequiredService<EndpointDataSource>();
            });

        var endpoint = endpoints!.Endpoints.OfType<RouteEndpoint>().Single(x => x.RoutePattern.RawText!.EndsWith(route, StringComparison.Ordinal));

        return (
            endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName,
            endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary,
            endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
    }

    private abstract class TestEndpoint<TGroup> : MinimalEndpoint<TGroup>
        where TGroup : ResourceGroup
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            throw new NotSupportedException();
        }
    }

    private abstract class TestEndpointV2<TGroup> : TestEndpoint<TGroup>
        where TGroup : ResourceGroup
    {
        public override int Version => 2;
    }

    private abstract class TestEndpointV3<TGroup> : TestEndpoint<TGroup>
        where TGroup : ResourceGroup
    {
        public override int Version => 3;
    }

    private sealed class OrdersGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, 2];
    }

    private sealed class OrdersV1 : TestEndpoint<OrdersGroup>;

    private sealed class OrdersV2 : TestEndpointV2<OrdersGroup>;

    private sealed class BillingGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [2, 3];
    }

    private sealed class BillingV2 : TestEndpointV2<BillingGroup>;

    private sealed class BillingV3 : TestEndpointV3<BillingGroup>;

    private sealed class DeprecatingGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, new(2, true, new DateOnly(2030, 1, 1))];
    }

    private sealed class DeprecatingV1 : TestEndpoint<DeprecatingGroup>;

    private sealed class DeprecatingV2 : TestEndpointV2<DeprecatingGroup>;

    private sealed class LaterSunsetGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [new(2, true, new DateOnly(2031, 1, 1))];
    }

    private sealed class LaterSunsetV2 : TestEndpointV2<LaterSunsetGroup>;

    private sealed class SameRouteNameGroup : ResourceGroup;

    private sealed class SameRouteNameFirst : TestEndpoint<SameRouteNameGroup>
    {
        public override string? Name => "same-route";
    }

    private sealed class SameRouteNameSecond : TestEndpoint<SameRouteNameGroup>
    {
        public override string? Name => "same-route";
    }

    private sealed class UndeclaredGroup : ResourceGroup;

    private sealed class UndeclaredV1 : TestEndpoint<UndeclaredGroup>;

    private sealed class UndeclaredV2 : TestEndpointV2<UndeclaredGroup>;

    private sealed class NoEndpointGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, 2];
    }

    private sealed class NoEndpointV1 : TestEndpoint<NoEndpointGroup>;

    private sealed class DuplicatedGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, 1];
    }

    private sealed class DuplicatedV1 : TestEndpoint<DuplicatedGroup>;

    private sealed class SunsetOnActiveGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [new(1, false, new DateOnly(2030, 1, 1))];
    }

    private sealed class SunsetOnActiveV1 : TestEndpoint<SunsetOnActiveGroup>;

    private sealed class FirstSameNameGroup : ResourceGroup
    {
        public override string Name => "same";
    }

    private sealed class SecondSameNameGroup : ResourceGroup
    {
        public override string Name => "same";
    }

    private sealed class NamesGroup : ResourceGroup
    {
        public override bool RequireAuthorization => false;
    }

    private sealed class DerivedNamesEndpoint : MinimalEndpoint<NamesGroup>
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("derived", () => "ok");
        }
    }

    private sealed class OverriddenNamesEndpoint : MinimalEndpoint<NamesGroup>
    {
        public override string? Name => "CustomName";

        public override string Summary => "Custom summary";

        public override string? Description => "Custom description";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("overridden", () => "ok");
        }
    }

    private sealed class FluentNamesEndpoint : MinimalEndpoint<NamesGroup>
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("fluent", () => "ok")
                .WithName("FluentName")
                .WithSummary("Fluent summary")
                .WithDescription("Fluent description");
        }
    }

    private sealed class FluentOverOverrideNamesEndpoint : MinimalEndpoint<NamesGroup>
    {
        public override string? Name => "IgnoredName";

        public override string Summary => "Ignored summary";

        public override string? Description => "Ignored description";

        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("fluent-over-override", () => "ok")
                .WithName("WinnerName")
                .WithSummary("Winner summary")
                .WithDescription("Winner description");
        }
    }

    private sealed class FirstDuplicateNameEndpoint : MinimalEndpoint<NamesGroup>
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("first-duplicate", () => "ok").WithName("Duplicated");
        }
    }

    private sealed class SecondDuplicateNameEndpoint : MinimalEndpoint<NamesGroup>
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            return builder.MapGet("second-duplicate", () => "ok").WithName("Duplicated");
        }
    }
}
