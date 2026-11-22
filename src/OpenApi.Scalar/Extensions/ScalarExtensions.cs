using System.Collections.Generic;
using System.Linq;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.OpenApi.Constants;
using Aurore.Foundation.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Aurore.Foundation.OpenApi.Scalar.Extensions;

/// <summary>
/// Configures the Scalar reference UI for the OpenAPI documents.
/// </summary>
public static class ScalarExtensions
{
    /// <summary>
    /// Registers one Scalar document per version, in the order given (the first one is the default).
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="versions">The API versions, in the order the documents are listed.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHostApplicationBuilder ConfigureStandardScalarDocuments(
        this IHostApplicationBuilder builder,
        IEnumerable<MinimalEndpointVersion> versions)
    {
        var documents = versions
            .Select(v => new ScalarDocument(OpenApiExtensions.DocumentName(v.Number), v switch
            {
                { IsDeprecated: true } => $"Version {v.Number} (deprecated)",
                _ => $"Version {v.Number}",
            }))
            .ToArray();

        builder.Services.Configure<ScalarOptions>(x => x.AddDocuments(documents));

        return builder;
    }

    /// <summary>
    /// Pre-configures the "Authorize" button for the OAuth2 authorization code flow with PKCE, so signing in from the
    /// documentation redirects to the identity provider without pasting anything. Does nothing without a client id.
    /// </summary>
    /// <param name="options">The Scalar options.</param>
    /// <param name="identityProvider">The identity provider the sign-in goes to.</param>
    /// <param name="apiDocs">The client id registered for the documentation.</param>
    /// <returns>The same options, for chaining.</returns>
    public static ScalarOptions WithStandardAuthentication(
        this ScalarOptions options,
        IdentityProviderOptions identityProvider,
        ApiDocsOptions apiDocs)
    {
        if (apiDocs.OAuthClientId.HasValue() is false)
            return options;

        return options
            .AddPreferredSecuritySchemes(SecuritySchemes.OAuth2)
            .AddAuthorizationCodeFlow(SecuritySchemes.OAuth2, flow => flow
                .WithClientId(apiDocs.OAuthClientId)
                .WithPkce(Pkce.Sha256)
                .WithSelectedScopes(identityProvider.Scopes.Keys));
    }
}
