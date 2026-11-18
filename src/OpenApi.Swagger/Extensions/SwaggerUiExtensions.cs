using System.Collections.Generic;
using System.Linq;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.OpenApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Aurore.Foundation.OpenApi.Swagger.Extensions;

/// <summary>
/// Configures Swagger UI for the OpenAPI documents of the application.
/// </summary>
public static class SwaggerUiExtensions
{
    /// <summary>
    /// Lists the API versions in Swagger UI, with their deprecation status, in the order given (the first one is the default).
    /// </summary>
    /// <param name="options">The UI options.</param>
    /// <param name="versions">The API versions, in the order they are listed.</param>
    /// <returns>The same options, for chaining.</returns>
    public static SwaggerUIOptions WithStandardDocuments(
        this SwaggerUIOptions options,
        IEnumerable<MinimalEndpointVersion> versions)
    {
        foreach (var version in versions)
        {
            var label = version.IsDeprecated ? $"Version {version.Number} (deprecated)" : $"Version {version.Number}";

            options.SwaggerEndpoint($"../openapi/{OpenApiExtensions.DocumentName(version.Number)}.json", label);
        }

        return options;
    }

    /// <summary>
    /// Pre-configures the "Authorize" button for the OAuth2 authorization code flow with PKCE, so signing in from the
    /// documentation redirects to the identity provider without pasting anything. Does nothing without a client id.
    /// </summary>
    /// <param name="options">The UI options.</param>
    /// <param name="identityProvider">The identity provider the sign-in goes to.</param>
    /// <param name="apiDocs">The client id registered for the documentation.</param>
    /// <returns>The same options, for chaining.</returns>
    public static SwaggerUIOptions WithStandardAuthentication(
        this SwaggerUIOptions options,
        IdentityProviderOptions identityProvider,
        ApiDocsOptions apiDocs)
    {
        if (apiDocs.OAuthClientId.HasValue() is false)
            return options;

        options.OAuthClientId(apiDocs.OAuthClientId);
        options.OAuthUsePkce();
        options.OAuthScopes(identityProvider.Scopes.Keys.ToArray());

        return options;
    }
}
