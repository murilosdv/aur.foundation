using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Constants;

/// <summary>
/// The SQL Server error numbers the foundation reacts to. Inherit from it to name the ones an application adds.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SqlServerErrorNumbers
{
    /// <summary>
    /// A row was refused by a unique index.
    /// </summary>
    public const int UniqueIndexViolation = 2601;

    /// <summary>
    /// A row was refused by a unique constraint or the primary key.
    /// </summary>
    public const int UniqueConstraintViolation = 2627;

    /// <summary>
    /// Lets an application inherit the constants; there is nothing to create.
    /// </summary>
    protected SqlServerErrorNumbers()
    {
    }
}
