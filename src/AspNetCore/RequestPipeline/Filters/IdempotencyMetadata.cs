using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Filters;

/// <summary>
/// Endpoint metadata added by WithIdempotency.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record IdempotencyMetadata(bool Required);
