using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Asp.Versioning;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.RequestPipeline.Filters;
using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.OpenApi.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Aurore.Foundation.OpenApi.Extensions;

/// <summary>
/// Configures the OpenAPI documents: document info, security schemes and the documented headers, whatever UI shows them.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// OpenAPI document name for a version (v1, v2...); built with the same format the API explorer uses for its groups,
    /// so a document always matches the endpoints of its version.
    /// </summary>
    /// <param name="version">The API version number.</param>
    public static string DocumentName(int version)
    {
        return new ApiVersion(version).ToString(VersioningConfiguration.GroupNameFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Fills the document's title, version, description and contact from the application information.
    /// </summary>
    /// <param name="options">The OpenAPI options.</param>
    /// <param name="version">The API version the document describes.</param>
    /// <param name="appInfo">The name, description and maintainer to show.</param>
    /// <returns>The same options, for chaining.</returns>
    public static OpenApiOptions AddStandardDocumentInfo(
        this OpenApiOptions options,
        MinimalEndpointVersion version,
        AppInformationOptions appInfo)
    {
        var notice = version switch
        {
            { IsDeprecated: true, Sunset: { } sunset } => $"**Deprecated.** This version stops being served on {sunset:yyyy-MM-dd}.",
            { IsDeprecated: true } => "**Deprecated.**",
            _ => null,
        };

        var description = string.Join("\n\n", new[] { appInfo.Description, version.Description, notice }.Where(x => string.IsNullOrWhiteSpace(x) is false));

        return options.AddDocumentTransformer((doc, _, _) =>
        {
            doc.Info.Title = appInfo.Component.ToTitleCase();
            doc.Info.Description = description;
            doc.Info.Version = $"{version.Number}.0";
            doc.Info.Contact = new()
            {
                Name = appInfo.Maintainer.Name,
                Email = appInfo.Maintainer.Email,
                Url = appInfo.Maintainer.Url is { Length: > 0 } url ? new Uri(url) : null
            };

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Adds a security scheme to the document and offers it on every operation that requires authorization, unless the
    /// endpoint pins other authentication schemes: an endpoint pinned to "ApiKey" lists only the API key, one pinned to
    /// "Bearer" lists the token schemes. Endpoints that pin nothing list every scheme.
    /// </summary>
    /// <param name="options">The OpenAPI options.</param>
    /// <param name="authenticationScheme">The ASP.NET Core authentication scheme this documented scheme stands for.</param>
    /// <param name="schema">The scheme to document.</param>
    /// <param name="scopes">The scopes to list with the requirement.</param>
    public static OpenApiOptions AddStandardSecuritySchemeAndRequirement(
        this OpenApiOptions options,
        string authenticationScheme,
        OpenApiSecurityScheme schema,
        params string[] scopes)
    {
        if (schema.Name.HasValue() is false)
            throw new ArgumentException("Security scheme must have a valid Name property.", nameof(schema));

        return options
            .AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes[schema.Name] = schema;

                return Task.CompletedTask;
            })
            .AddOperationTransformer((operation, context, _) =>
            {
                // Group-level and endpoint-level RequireAuthorization()/AllowAnonymous() both end up in the endpoint metadata.
                if (RequiresAuthorization(context.Description) is false || AcceptsScheme(context.Description, authenticationScheme) is false)
                    return Task.CompletedTask;

                operation.Security ??= [];

                operation.Security.Add(new()
                {
                    { new(schema.Name, context.Document), [.. scopes] }
                });

                return Task.CompletedTask;
            });
    }

    // The schemes a policy or attribute pins are the only ones ASP.NET Core tries for the endpoint; with none pinned it uses
    // the default scheme, which accepts them all.
    private static bool AcceptsScheme(ApiDescription description, string authenticationScheme)
    {
        var metadata = description.ActionDescriptor.EndpointMetadata;

        var pinned = metadata.OfType<IAuthorizeData>()
            .Select(x => x.AuthenticationSchemes)
            .Where(x => x.HasValue())
            .SelectMany(x => x!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Concat(metadata.OfType<AuthorizationPolicy>().SelectMany(x => x.AuthenticationSchemes))
            .ToList();

        return pinned.Count is 0 || pinned.Contains(authenticationScheme, StringComparer.OrdinalIgnoreCase);
    }

    private static bool RequiresAuthorization(ApiDescription description)
    {
        var metadata = description.ActionDescriptor.EndpointMetadata;

        return metadata.OfType<IAuthorizeData>().Any() && metadata.OfType<IAllowAnonymous>().Any() is false;
    }

    /// <summary>
    /// Documents bearer token authentication.
    /// </summary>
    public static OpenApiOptions AddStandardJwtBearerSecurity(this OpenApiOptions options)
    {
        return options.AddStandardSecuritySchemeAndRequirement(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = SecuritySchemes.JwtBearer,
            Description = "JWT Authorization header using the Bearer scheme.",
            In = ParameterLocation.Header,
            Scheme = SecuritySchemes.JwtBearer.ToLowerInvariant(),
            BearerFormat = "JWT"
        });
    }

    /// <summary>
    /// For an apiKey scheme OpenAPI uses Name as the header name, so the scheme is listed under the header name too.
    /// </summary>
    /// <param name="options">The OpenAPI options.</param>
    /// <param name="apiKeys">The API key settings. Nothing is documented when there are no clients.</param>
    /// <returns>The same options, for chaining.</returns>
    public static OpenApiOptions AddStandardApiKeySecurity(this OpenApiOptions options, ApiKeysOptions apiKeys)
    {
        if (apiKeys.Clients.Count is 0)
            return options;

        return options.AddStandardSecuritySchemeAndRequirement(ApiKeyDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = apiKeys.HeaderName,
            Description = $"API key in the {apiKeys.HeaderName} header.",
            In = ParameterLocation.Header,
        });
    }

    /// <summary>
    /// Documents OpenID Connect authentication against the configured identity provider.
    /// </summary>
    /// <param name="options">The OpenAPI options.</param>
    /// <param name="identityProvider">The identity provider whose discovery document is linked.</param>
    /// <returns>The same options, for chaining.</returns>
    public static OpenApiOptions AddStandardOidcSecurity(
        this OpenApiOptions options,
        IdentityProviderOptions identityProvider)
    {
        return options.AddStandardSecuritySchemeAndRequirement(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OpenIdConnect,
            Name = SecuritySchemes.Oidc,
            Description = "OIDC Authorization header using the Bearer scheme.",
            OpenIdConnectUrl = new Uri($"{identityProvider.AuthorityUrl.TrimEnd('/')}/.well-known/openid-configuration")
        });
    }

    /// <summary>
    /// Documents the OAuth2 authorization code flow against the configured identity provider.
    /// </summary>
    /// <param name="options">The OpenAPI options.</param>
    /// <param name="identityProvider">The identity provider: authorization and token paths and the scopes to list.</param>
    /// <returns>The same options, for chaining.</returns>
    public static OpenApiOptions AddStandardOAuth2Security(
        this OpenApiOptions options,
        IdentityProviderOptions identityProvider)
    {
        var authority = identityProvider.AuthorityUrl.TrimEnd('/');
        var tokenUrl = new Uri($"{authority}/{identityProvider.TokenPath.TrimStart('/')}");

        return options.AddStandardSecuritySchemeAndRequirement(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Name = SecuritySchemes.OAuth2,
            Description = "OAuth2 Authorization Code Flow",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority}/{identityProvider.AuthorizationPath.TrimStart('/')}"),
                    TokenUrl = tokenUrl,

                    RefreshUrl = tokenUrl,
                    Scopes = identityProvider.Scopes
                }
            }
        });
    }

    /// <summary>
    /// Writes the names of the query parameters in camel case ("pageSize", not "PageSize"), the same rule the JSON of the
    /// bodies follows. The generator uses the C# property names for them, which the JSON naming policy never touches.
    /// Binding ignores case, so this only changes what the document says. Headers and route values are left as they are.
    /// </summary>
    public static OpenApiOptions AddStandardCamelCaseQueryParameters(this OpenApiOptions options)
    {
        return options.AddOperationTransformer((operation, _, _) =>
        {
            var parameters = operation.Parameters?.OfType<OpenApiParameter>() ?? [];

            foreach (var parameter in parameters.Where(IsNamedQueryParameter))
            {
                parameter.Name = parameter.Name.ToCamelCase();
            }

            return Task.CompletedTask;
        });

        static bool IsNamedQueryParameter(OpenApiParameter parameter)
        {
            return parameter.In is ParameterLocation.Query && parameter.Name.HasValue();
        }
    }

    /// <summary>
    /// Documents the Idempotency-Key request header on the operations that use it, those marked with WithIdempotency, and
    /// only there. It is required when the endpoint was marked required: true, optional otherwise.
    /// </summary>
    public static OpenApiOptions AddStandardIdempotencyKeyHeader(this OpenApiOptions options)
    {
        return options.AddOperationTransformer((operation, context, _) =>
        {
            var idempotency = context.Description.ActionDescriptor.EndpointMetadata.OfType<IdempotencyMetadata>().FirstOrDefault();

            if (idempotency is null)
                return Task.CompletedTask;

            operation.Parameters ??= [];

            if (operation.Parameters.All(p => p.Name is not CustomHeaderNames.IdempotencyKey))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = CustomHeaderNames.IdempotencyKey,
                    In = ParameterLocation.Header,
                    Required = idempotency.Required,
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Pattern = RequestContextMiddleware.IdempotencyKeyPattern,
                        MinLength = RequestContextMiddleware.MinIdempotencyKeyLength,
                        MaxLength = RequestContextMiddleware.MaxIdempotencyKeyLength,
                        Examples = [JsonValue.Create(Guid.NewGuid().ToString())]
                    },
                    Description = $"Idempotency Key for request idempotence. Letters, digits, '.', '_' and '-', {RequestContextMiddleware.MinIdempotencyKeyLength} to {RequestContextMiddleware.MaxIdempotencyKeyLength} characters."
                });
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Documents the optional Correlation-Id request header on every operation.
    /// </summary>
    public static OpenApiOptions AddStandardCorrelationIdHeader(this OpenApiOptions options)
    {
        return options.AddDocumentTransformer((document, _, _) =>
        {
            foreach (var operation in document.Paths.Values.SelectMany(x => (IEnumerable<OpenApiOperation>?)x.Operations?.Values ?? []))
            {
                operation.Parameters ??= [];

                if (operation.Parameters.All(p => p.Name is not CustomHeaderNames.CorrelationId))
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = CustomHeaderNames.CorrelationId,
                        In = ParameterLocation.Header,
                        Required = false,
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Pattern = RequestContextMiddleware.CorrelationIdPattern,
                            MinLength = RequestContextMiddleware.MinCorrelationIdLength,
                            MaxLength = RequestContextMiddleware.MaxCorrelationIdLength,
                            Examples = [JsonValue.Create(Guid.NewGuid().ToString())]
                        },
                        Description = $"Correlation Id for request tracking and tracing. Letters, digits, '.', '_' and '-', {RequestContextMiddleware.MinCorrelationIdLength} to {RequestContextMiddleware.MaxCorrelationIdLength} characters; a new one is generated when missing or invalid."
                    });
                }
            }

            return Task.CompletedTask;
        });
    }
}
