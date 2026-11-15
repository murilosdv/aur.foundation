using System.Threading.Tasks;
using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Support;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Contracts;

[Trait("Category", "Integration")]
public sealed class SqlServerSoftDeleteTests(SqlServerFixture database) : SoftDeleteContract<SqlServerFixture>(database);

[Trait("Category", "Integration")]
public sealed class SqlServerTimestampTests(SqlServerFixture database) : TimestampContract<SqlServerFixture>(database);

[Trait("Category", "Integration")]
public sealed class SqlServerDbContextTests(SqlServerFixture database) : DbContextContract<SqlServerFixture>(database);

[Trait("Category", "Integration")]
public sealed class SqlServerPagingTests(SqlServerPagingTests.PeopleFixture database) : PagingContract<SqlServerPagingTests.PeopleFixture>(database)
{
    /// <summary>
    /// The 45 people the paging tests read, seeded once for the class.
    /// </summary>
    public sealed class PeopleFixture : SqlServerFixture
    {
        protected override ValueTask SeedAsync(TestDbContext db)
        {
            return SeedPeopleAsync(db);
        }
    }
}

[Trait("Category", "Unit")]
public sealed class SqlServerModelConventionsTests : ModelConventionsContract
{
    protected override DbContextOptions<TestDbContext> Options => new DbContextOptionsBuilder<TestDbContext>().UseSqlServer("Server=localhost").Options;

    protected override string NotDeletedFilter => "[deleted_at] IS NULL";
}
