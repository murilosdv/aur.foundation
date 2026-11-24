using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.HttpDependencies.Handlers;
using Aurore.Foundation.Tests.AspNetCore.Support;

namespace Aurore.Foundation.Tests.AspNetCore.HttpDependencies.Handlers;

[Trait("Feature", "Outbound HTTP handlers")]
public sealed class BearerTokenHandlerTests
{
    [Theory(DisplayName = "The token is sent with the Bearer prefix exactly once, whether or not it came with it")]
    [InlineData("static-token")]
    [InlineData("Bearer static-token")]
    [InlineData("bearer static-token")]
    [InlineData("BEARER static-token")]
    [InlineData("  Bearer   static-token  ")]
    [InlineData("Bearer Bearer static-token")]
    [InlineData("Bearer\tstatic-token")]
    public async Task TokenIsSentWithThePrefixOnce(string token)
    {
        // Arrange
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var invoker = new HttpMessageInvoker(new BearerTokenHandler(token) { InnerHandler = inner });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://upstream.example/items");

        // Act
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Bearer static-token", inner.Requests[0].Headers.Authorization!.ToString());
    }

    [Fact(DisplayName = "The token replaces an Authorization header the caller set")]
    public async Task TokenReplacesTheCallersHeader()
    {
        // Arrange
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var invoker = new HttpMessageInvoker(new BearerTokenHandler("static-token") { InnerHandler = inner });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://upstream.example/items");
        request.Headers.TryAddWithoutValidation("Authorization", "Basic from-the-caller");

        // Act
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Bearer static-token"], inner.Requests[0].Headers.GetValues("Authorization"));
    }

    [Theory(DisplayName = "A token that only starts with the word Bearer keeps it")]
    [InlineData("Bearerabc", "Bearerabc")]
    [InlineData("Bearer Bearerabc", "Bearerabc")]
    public void WordInsideTheTokenIsKept(string token, string expected)
    {
        // Act
        var result = BearerTokenHandler.Normalize(token);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "No token, blank or only the prefix, normalizes to empty")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bearer")]
    [InlineData("Bearer   ")]
    [InlineData("bearer bearer")]
    public void NoTokenIsEmpty(string? token)
    {
        // Act
        var result = BearerTokenHandler.Normalize(token);

        // Assert
        Assert.Empty(result);
    }
}
