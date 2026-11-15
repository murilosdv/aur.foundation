using System.Threading.Tasks;
using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Support;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Contracts;

[Trait("Category", "Integration")]
public sealed class PostgresSoftDeleteTests(PostgresFixture database) : SoftDeleteContract<PostgresFixture>(database);

[Trait("Category", "Integration")]
public sealed class PostgresTimestampTests(PostgresFixture database) : TimestampContract<PostgresFixture>(database);

[Trait("Category", "Integration")]
public sealed class PostgresDbContextTests(PostgresFixture database) : DbContextContract<PostgresFixture>(database);

[Trait("Category", "Integration")]
public sealed class PostgresPagingTests(PostgresPagingTests.PeopleFixture database) : PagingContract<PostgresPagingTests.PeopleFixture>(database)
{
    /// <summary>
    /// The 45 people the paging tests read, seeded once for the class.
    /// </summary>
    public sealed class PeopleFixture : PostgresFixture
    {
        protected override ValueTask SeedAsync(TestDbContext db)
        {
            return SeedPeopleAsync(db);
        }
    }
}

[Trait("Category", "Unit")]
public sealed class PostgresModelConventionsTests : ModelConventionsContract
{
    protected override DbContextOptions<TestDbContext> Options => new DbContextOptionsBuilder<TestDbContext>().UseNpgsql("Host=localhost").Options;

    protected override string NotDeletedFilter => "\"deleted_at\" IS NULL";
}
