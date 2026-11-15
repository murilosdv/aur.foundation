using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Constants;

/// <summary>
/// The PostgreSQL error codes (SQLSTATE) the foundation reacts to. Inherit from it to name the ones an application adds.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class PostgresSqlStates
{
    /// <summary>
    /// A unique constraint or unique index was violated.
    /// </summary>
    public const string UniqueViolation = "23505";

    /// <summary>
    /// Lets an application inherit the constants; there is nothing to create.
    /// </summary>
    protected PostgresSqlStates()
    {
    }
}
