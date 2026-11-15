using System.Linq;
using System.Text.RegularExpressions;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Constants;
using Microsoft.Data.SqlClient;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;

internal static partial class SqlExceptionExtensions
{
    /// <summary>
    /// Reads the name of the violated unique index (error 2601) or constraint (error 2627) from the message of the server, the
    /// only place it exists. It only works with the messages in English: with any other language it answers false.
    /// </summary>
    internal static bool TryGetUniqueIndexName(this SqlException exception, out string name)
    {
        foreach (var error in exception.Errors.Cast<SqlError>())
        {
            var match = error.Number switch
            {
                SqlServerErrorNumbers.UniqueIndexViolation => IndexName().Match(error.Message),
                SqlServerErrorNumbers.UniqueConstraintViolation => ConstraintName().Match(error.Message),
                _ => Match.Empty
            };

            if (match.Success)
            {
                name = match.Groups[1].Value;

                return true;
            }
        }

        name = string.Empty;

        return false;
    }

    [GeneratedRegex("with unique index '([^']+)'")]
    private static partial Regex IndexName();

    [GeneratedRegex("constraint '([^']+)'")]
    private static partial Regex ConstraintName();
}
