using System.Linq;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.EntityFrameworkCore.Errors;
using Aurore.Foundation.EntityFrameworkCore.Features;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Constants;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Features;

/// <summary>
/// Answers with a Conflict result when saving a feature's changes violates a unique index or constraint (errors 2601 and 2627).
/// The error is the one registered for the index in the <see cref="UniqueViolationMap"/>, else <see cref="DatabaseErrors.Conflict"/>
/// with the index's fields read from the model. SQL Server gives the index name only in the English message text; in another
/// language the answer is the plain <see cref="DatabaseErrors.Conflict"/>, without the registered error or fields.
/// </summary>
public sealed partial class UniqueViolationDecorator(ILogger<UniqueViolationDecorator> logger) : IFeatureDecorator
{
    /// <inheritdoc />
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        try
        {
            return await next();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql
            && sql.Errors.Cast<SqlError>().Any(error => error.Number is SqlServerErrorNumbers.UniqueIndexViolation or SqlServerErrorNumbers.UniqueConstraintViolation))
        {
            LogUniqueViolation(context.FeatureName, sql.Number, sql.Message);

            var indexName = sql.TryGetUniqueIndexName(out var name) ? name : default(string);
            var map = context.Services.GetService<UniqueViolationMap>();

            return Result.Failure(map?.Find(indexName) ?? UniqueViolationErrors.Describe(exception, indexName));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Feature {Feature} encountered SQL Server duplicate error {ErrorNumber}: {SqlMessage}")]
    private partial void LogUniqueViolation(string feature, int errorNumber, string sqlMessage);
}
