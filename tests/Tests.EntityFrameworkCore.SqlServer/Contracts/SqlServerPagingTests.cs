using System.Threading.Tasks;
using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Support;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Contracts;

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
