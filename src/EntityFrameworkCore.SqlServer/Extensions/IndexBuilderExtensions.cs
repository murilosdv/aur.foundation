using Aurore.Foundation.Core.Entities;
using Aurore.Foundation.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;

/// <summary>
/// SQL Server filtered-index helpers.
/// </summary>
public static class IndexBuilderExtensions
{
    /// <summary>
    /// Restricts an index to nondeleted rows, using the snake-case column name from shared conventions.
    /// </summary>
    /// <typeparam name="TEntity">The soft-deletable entity.</typeparam>
    /// <param name="index">The index being configured.</param>
    public static IndexBuilder<TEntity> WhereNotDeleted<TEntity>(this IndexBuilder<TEntity> index) where TEntity : ISoftDeletable
    {
        return index.HasFilter($"[{nameof(ISoftDeletable.DeletedAt).ToSnakeCase()}] IS NULL");
    }
}
