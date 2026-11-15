using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;

/// <summary>
/// SQL Server table layout helpers.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// The name of the shadow column that holds the clustering order.
    /// </summary>
    public const string SequentialIdProperty = "SequentialId";

    /// <summary>
    /// Keeps the Guid primary key but makes it non-clustered, and clusters the table on a shadow bigint identity column instead,
    /// so rows are appended at the end of the table. SQL Server sorts a Guid by its last bytes, so even a version 7 Guid
    /// does not arrive in order in a clustered index. The column is not part of the entity class.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <param name="builder">The builder of the entity type. Its table and primary key must already be set.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static EntityTypeBuilder<TEntity> UseSequentialClusteredIndex<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        var key = builder.Metadata.FindPrimaryKey()
            ?? throw new System.InvalidOperationException($"{typeof(TEntity).Name} has no primary key: set it before using a sequential clustered index.");

        builder.HasKey([.. key.Properties.Select(x => x.Name)]).IsClustered(false);

        builder.Property<long>(SequentialIdProperty).ValueGeneratedOnAdd().UseIdentityColumn();

        builder
            .HasIndex(SequentialIdProperty)
            .IsUnique()
            .IsClustered()
            .HasDatabaseName($"CX_{builder.Metadata.GetTableName()}");

        return builder;
    }
}
