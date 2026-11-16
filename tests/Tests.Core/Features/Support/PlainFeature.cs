using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;

namespace Aurore.Foundation.Tests.Core.Features.Support;

public sealed class PlainFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success($"hello {request.Name}"));
    }
}
