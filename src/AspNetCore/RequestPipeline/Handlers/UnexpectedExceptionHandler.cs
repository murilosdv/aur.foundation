using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Handlers;

/// <summary>
/// Catch-all for unplanned failures; expected outcomes are returned as results, not thrown. A caller that left gets a 499 (logged at
/// Debug), a malformed request keeps the status the framework chose (logged as a warning), anything else is a generic 500 logged as an
/// error. The response never contains exception details.
/// </summary>
internal sealed class UnexpectedExceptionHandler(ILogger<UnexpectedExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// The 499 status (used by nginx) for a caller that disconnected before the response was ready.
    /// </summary>
    public const int ClientClosedRequestStatusCode = 499;

    /// <summary>
    /// Writes the problem response for the exception. Always handles it, so nothing falls through to the framework's
    /// default page.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="exception">The exception that escaped.</param>
    /// <param name="cancellationToken">A token to cancel the response.</param>
    /// <returns>True: the exception is always handled here.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested:
                logger.LogDebug("The caller closed the request before it completed.");
                httpContext.Response.StatusCode = ClientClosedRequestStatusCode;

                return true;

            case BadHttpRequestException badRequest:
                logger.LogWarning("Malformed request rejected with {StatusCode}: {Reason}", badRequest.StatusCode, badRequest.Message);
                await WriteProblemAsync(httpContext, badRequest.StatusCode, "The request could not be processed.");

                return true;

            default:
                logger.LogError(exception, "An unexpected error occurred.");
                await WriteProblemAsync(httpContext, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");

                return true;
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        return TypedResults
            .Problem(statusCode: statusCode, title: ReasonPhrases.GetReasonPhrase(statusCode), detail: detail)
            .ExecuteAsync(context);
    }
}
