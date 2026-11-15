using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Entities;
using Aurore.Foundation.EntityFrameworkCore.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Extensions;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Configuration;

[Trait("Feature", "Sequential clustered index")]
[Trait("Category", "Integration")]
public sealed class SequentialClusteredTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Fact(DisplayName = "The Guid primary key is not clustered, and the table is clustered on a sequential column")]
    public async Task KeyIsNonClusteredAndTheTableIsClusteredOnTheSequentialColumn()
    {
        // Arrange
        await using var db = await CreateAsync();

        // Act
        var indexes = await ReadIndexesAsync();

        // Assert
        Assert.Equal("NONCLUSTERED", indexes["PK_ledger_entries"]);
        Assert.Equal("CLUSTERED", indexes["CX_ledger_entries"]);
    }

    [Fact(DisplayName = "Rows come back in the order they were inserted when the table is scanned")]
    public async Task RowsAreStoredInInsertionOrder()
    {
        // Arrange
        await using var db = await CreateAsync();
        var labels = new List<string>();

        for (var i = 0; i < 30; i++)
        {
            labels.Add($"entry-{i:D2}");
            db.Entries.Add(new LedgerEntry { Label = labels[^1], Id = Guid.NewGuid() });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var scanned = await ScanAsync();

        // Assert
        Assert.Equal(labels, scanned);
    }

    private async Task<LedgerContext> CreateAsync()
    {
        var db = new LedgerContext(new DbContextOptionsBuilder<LedgerContext>().UseSqlServer(database.Options.ToConnectionString()).Options);
        await db.Database.ExecuteSqlRawAsync("IF OBJECT_ID('app.ledger_entries') IS NOT NULL DROP TABLE app.ledger_entries", TestContext.Current.CancellationToken);
        await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(TestContext.Current.CancellationToken);

        return db;
    }

    private async Task<Dictionary<string, string>> ReadIndexesAsync()
    {
        await using var connection = new SqlConnection(database.Options.ToConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new SqlCommand("SELECT name, type_desc FROM sys.indexes WHERE object_id = OBJECT_ID('app.ledger_entries') AND name IS NOT NULL", connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            result[reader.GetString(0)] = reader.GetString(1);

        return result;
    }

    private async Task<List<string>> ScanAsync()
    {
        await using var connection = new SqlConnection(database.Options.ToConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new SqlCommand("SELECT label FROM app.ledger_entries", connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var result = new List<string>();

        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            result.Add(reader.GetString(0));

        return result;
    }

    private sealed class LedgerEntry : Entity
    {
        public required string Label { get; set; }
    }

    private sealed class LedgerEntryConfiguration : EntityConfigurationBase<LedgerEntry>
    {
        protected override string TableName => "LedgerEntries";

        protected override void Extend(EntityTypeBuilder<LedgerEntry> builder)
        {
            builder.UseSequentialClusteredIndex();
            builder.Property(x => x.Label).HasMaxLength(32);
        }
    }

    private sealed class LedgerContext(DbContextOptions<LedgerContext> options) : DbContext(options)
    {
        public DbSet<LedgerEntry> Entries => Set<LedgerEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LedgerEntryConfiguration()).ApplyStandardConventions();
        }
    }
}
