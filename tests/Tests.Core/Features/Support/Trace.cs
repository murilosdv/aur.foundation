using System.Collections.Generic;

namespace Aurore.Foundation.Tests.Core.Features.Support;

/// <summary>
/// What happened during a run, in order. Registered as a singleton so tests can read it afterwards.
/// </summary>
public sealed class Trace
{
    public List<string> Steps { get; } = [];
}
