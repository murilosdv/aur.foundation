using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Aurore.Foundation.Tests.AspNetCore.Support;

/// <summary>
/// Keeps the callbacks registered with Response.OnStarting and runs them on demand: a DefaultHttpContext has no server to start the
/// response, so without this the headers that components add when the response starts are never added.
/// </summary>
internal sealed class ResponseStartFeature : HttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object State)> _callbacks = [];

    /// <summary>
    /// Replaces the response feature of the context with one that captures the callbacks.
    /// </summary>
    public static ResponseStartFeature Install(HttpContext http)
    {
        var feature = new ResponseStartFeature();
        http.Features.Set<IHttpResponseFeature>(feature);

        return feature;
    }

    /// <inheritdoc />
    public override void OnStarting(Func<object, Task> callback, object state)
    {
        _callbacks.Add((callback, state));
    }

    /// <summary>
    /// Runs the callbacks, as the server does when the response starts.
    /// </summary>
    public async Task StartAsync()
    {
        foreach (var (callback, state) in _callbacks)
            await callback(state);
    }
}
