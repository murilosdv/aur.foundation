using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Aurore.Foundation.AspNetCore.MinimalApis.Abstractions;
using Aurore.Foundation.AspNetCore.RequestPipeline.Filters;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Aurore.Foundation.AspNetCore.MinimalApis.Configuration;

/// <summary>
/// Discovers the resource groups and endpoints of the application and maps them.
/// </summary>
public static class MinimalApiConfiguration
{
    /// <summary>
    /// Default problem responses every endpoint can return, documented once for the whole API. 400 is documented
    /// separately as a validation problem (with the "errors" dictionary). Pass your own list to
    /// <see cref="MapStandardMinimalEndpoints"/> to replace these.
    /// </summary>
    public static readonly IReadOnlyList<int> StandardProblems =
    [
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status429TooManyRequests,
        StatusCodes.Status500InternalServerError,
    ];

    /// <summary>
    /// Discovers and validates every resource group and endpoint in the assembly of <typeparamref name="TAssembly"/>.
    /// The returned catalog is the single source of truth for mapping and API documents. Throws when the configuration
    /// is inconsistent (endpoint targeting an undeclared version, version without endpoints, duplicate group names...).
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="catalog">The catalog of the resource groups and endpoints found.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="InvalidOperationException">The declarations are inconsistent.</exception>
    public static IHostApplicationBuilder AddStandardMinimalEndpoints<TAssembly>(this IHostApplicationBuilder builder, out MinimalEndpointCatalog catalog)
    {
        catalog = MinimalEndpointCatalog.Create(typeof(TAssembly).Assembly);

        return builder;
    }

    /// <summary>
    /// Maps the endpoints of the catalog built by <see cref="AddStandardMinimalEndpoints{TAssembly}"/>.
    /// </summary>
    /// <param name="builder">The route builder (usually the WebApplication).</param>
    /// <param name="catalog">The catalog returned by AddStandardMinimalEndpoints.</param>
    /// <param name="configure">Conventions applied to every endpoint.</param>
    /// <param name="globalProblems">
    /// Problem status codes (400-599) documented on every endpoint. Replaces <see cref="StandardProblems"/> when given;
    /// null keeps the defaults.
    /// </param>
    public static IEndpointRouteBuilder MapStandardMinimalEndpoints(
        this IEndpointRouteBuilder builder, MinimalEndpointCatalog catalog, Action<RouteGroupBuilder>? configure = null, IEnumerable<int>? globalProblems = null)
    {
        var problems = ResolveGlobalProblems(globalProblems);

        var api = builder
            .MapGroup(string.Empty)
            .ProducesValidationProblem()
            .AddEndpointFilterFactory(RequestValidationFilter.Create);

        foreach (var problem in problems)
            api.ProducesProblem(problem);

        configure?.Invoke(api);

        foreach (var (group, endpoints) in catalog.Resources)
        {
            var prefix = group.PrefixWithApi ? "api/v{version:apiVersion}" : "v{version:apiVersion}";

            var groupBuilder = api
                .MapGroup($"{prefix}/{group.Name}")
                .WithTags([.. group.Tags.Where(x => x.HasValue())])
                .ConfigureProblems(group, problems)
                .ConfigureVersionSet(group)
                .ConfigureAuthorization(group);

            group.Configure(groupBuilder);

            groupBuilder.MapEndpoints(endpoints);
        }

        api.ValidateRouteNames();

        return builder;
    }

    /// <summary>
    /// The catalog only knows the names from the Name property; a WithName call inside Map is a convention that ASP.NET applies
    /// when it builds the endpoints. Building them here, while the application is still starting, makes a duplicate stop the start
    /// instead of answering 500 on the first request.
    /// </summary>
    private static void ValidateRouteNames(this IEndpointRouteBuilder api)
    {
        var duplicate = api.DataSources
            .SelectMany(x => x.Endpoints)
            .Select(x => (Endpoint: x, Name: x.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName))
            .Where(x => x.Name is not null)
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicate is null)
            return;

        var names = string.Join(", ", duplicate.Select(x => x.Endpoint.DisplayName));

        throw new InvalidOperationException($"Endpoints [{names}] share the route name '{duplicate.Key}'. Use a different name in WithName or in the Name property.");
    }

    internal static IReadOnlyList<int> ResolveGlobalProblems(IEnumerable<int>? globalProblems)
    {
        if (globalProblems?.Distinct().ToArray() is not { } custom)
            return StandardProblems;

        if (custom.Where(x => x is < 400 or > 599).ToArray() is { Length: > 0 } invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(globalProblems), $"Problem status codes must be between 400 and 599 (got [{string.Join(", ", invalid)}]).");
        }

        return custom;
    }

    private static RouteGroupBuilder ConfigureProblems(
        this RouteGroupBuilder builder,
        ResourceGroup group,
        IReadOnlyList<int> globalProblems)
    {
        foreach (var problem in group.Problems.Except(globalProblems))
            builder.ProducesProblem(problem);

        return builder;
    }

    private static RouteGroupBuilder ConfigureVersionSet(this RouteGroupBuilder builder, ResourceGroup group)
    {
        var set = builder.NewApiVersionSet();

        foreach (var version in group.Versions)
        {
            var apiVersion = new ApiVersion(version.Number);

            _ = version switch
            {
                { IsDeprecated: true } => set.HasDeprecatedApiVersion(apiVersion),
                _ => set.HasApiVersion(apiVersion),
            };
        }

        builder.WithApiVersionSet(set.Build());

        return builder;
    }

    private static RouteGroupBuilder ConfigureAuthorization(this RouteGroupBuilder builder, ResourceGroup group)
    {
        return group.RequireAuthorization ? builder.RequireAuthorization() : builder;
    }

    private static void MapEndpoints(this RouteGroupBuilder builder, IReadOnlyList<IMinimalEndpoint> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var route = endpoint
                .Map(builder)
                .MapToApiVersion(endpoint.Version);

            // Precedence, last wins: derived default < property override < WithName/WithSummary/WithDescription inside Map.
            // Conventions run in the order they were added, so the ones from Map have already run when these do.
            route.Add(x =>
            {
                if (x.Metadata.OfType<IEndpointNameMetadata>().Any() is false)
                    x.Metadata.Add(new EndpointNameMetadata(endpoint.RouteName));

                if (x.Metadata.OfType<IEndpointSummaryMetadata>().Any() is false)
                    x.Metadata.Add(new EndpointSummaryAttribute(endpoint.Summary));

                if (endpoint.Description is { } description && x.Metadata.OfType<IEndpointDescriptionMetadata>().Any() is false)
                    x.Metadata.Add(new EndpointDescriptionAttribute(description));
            });
        }
    }
}
