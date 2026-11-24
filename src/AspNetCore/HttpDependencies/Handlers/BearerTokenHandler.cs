using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Aurore.Foundation.AspNetCore.HttpDependencies.Handlers;

/// <summary>
/// Adds a static token as <c>Authorization: Bearer &lt;token&gt;</c> to every request.
/// </summary>
internal sealed class BearerTokenHandler(string token) : DelegatingHandler
{
    private const string Scheme = "Bearer";

    private readonly string _token = Normalize(token);

    /// <summary>
    /// Returns the token without the "Bearer" prefix (any case, once or repeated) and the surrounding whitespace, so the
    /// header carries the prefix exactly once whether or not the configured value had it.
    /// </summary>
    /// <param name="token">The token, with or without the prefix.</param>
    /// <returns>The bare token, empty when there is none.</returns>
    internal static string Normalize(string? token)
    {
        var value = token?.Trim() ?? string.Empty;

        while (HasScheme(value))
            value = value[Scheme.Length..].TrimStart();

        return value;

        // "Bearer" alone or followed by whitespace; "Bearerabc" is a token that happens to start with the word.
        static bool HasScheme(string value)
        {
            return value.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase)
                && (value.Length == Scheme.Length || char.IsWhiteSpace(value[Scheme.Length]));
        }
    }

    /// <summary>
    /// Sets the Authorization header before sending the request, replacing one the caller set.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, _token);

        return base.SendAsync(request, cancellationToken);
    }
}
