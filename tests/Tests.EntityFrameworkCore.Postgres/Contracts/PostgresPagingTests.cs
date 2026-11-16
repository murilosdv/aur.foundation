using System.Threading.Tasks;
using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Support;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Contracts;

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
