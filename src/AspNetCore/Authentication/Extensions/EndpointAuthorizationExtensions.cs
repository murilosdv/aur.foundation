using System.Linq;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;

namespace Aurore.Foundation.AspNetCore.Authentication.Extensions;

/// <summary>
/// Authentication scheme, role and scope requirements for endpoints and resource groups. Requirements from every level
/// must all pass; explicitly selected authentication schemes are combined as alternatives.
/// </summary>
public static class EndpointAuthorizationExtensions
{
    /// <summary>
    /// Requires an authenticated JWT bearer caller, excluding other registered authentication schemes.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    /// <typeparam name="TBuilder">The endpoint convention builder.</typeparam>
    /// <returns>The same builder, for chaining role or scope requirements.</returns>
    public static TBuilder RequireJwtBearerAuthentication<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());
    }

    /// <summary>
    /// Requires an authenticated API-key caller, excluding other registered authentication schemes.
    /// The API-key scheme must have been registered in the application.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    /// <typeparam name="TBuilder">The endpoint convention builder.</typeparam>
    /// <returns>The same builder, for chaining role or scope requirements.</returns>
    public static TBuilder RequireApiKeyAuthentication<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy
            .AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());
    }

    /// <summary>
    /// Requires the caller to have EVERY one of the roles.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    /// <param name="roles">The roles the caller must have, all of them.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder RequireRoles<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy =>
        {
            foreach (var role in roles)
                policy.RequireRole(role);
        });
    }

    /// <summary>
    /// Requires the caller to have AT LEAST ONE of the roles.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    /// <param name="roles">The roles of which the caller must have at least one.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder RequireAnyRole<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy.RequireRole(roles));
    }

    /// <summary>
    /// Requires the caller to have EVERY one of the scopes.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    /// <param name="scopes">The scopes the caller must have, all of them.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder RequireScopes<TBuilder>(this TBuilder builder, params string[] scopes)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => scopes.All(context.User.HasScope)));
    }

    /// <summary>
    /// Requires the caller to have AT LEAST ONE of the scopes.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    /// <param name="scopes">The scopes of which the caller must have at least one.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder RequireAnyScope<TBuilder>(this TBuilder builder, params string[] scopes)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => scopes.Any(context.User.HasScope)));
    }
}
