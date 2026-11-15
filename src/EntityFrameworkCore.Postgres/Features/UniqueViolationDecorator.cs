using System.Threading.Tasks;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.EntityFrameworkCore.Errors;
using Aurore.Foundation.EntityFrameworkCore.Features;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

/// <summary>
/// Answers with a Conflict result when saving a feature's changes violates a unique constraint (state 23505), instead of letting the
/// exception escape. The error is the one registered for the constraint in the <see cref="UniqueViolationMap"/>, else
/// <see cref="DatabaseErrors.Conflict"/> with the violated fields read from the model; the constraint name never reaches the client.
/// Postgres aborts the transaction on a violation, so a feature that runs more statements in it must check first.
/// </summary>
public sealed class UniqueViolationDecorator : IFeatureDecorator
{
    /// <inheritdoc />
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        try
        {
            return await next();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresSqlStates.UniqueViolation } violation)
        {
            var map = context.Services.GetService<UniqueViolationMap>();

            return Result.Failure(map?.Find(violation.ConstraintName) ?? UniqueViolationErrors.Describe(ex, violation.ConstraintName));
        }
    }

}
