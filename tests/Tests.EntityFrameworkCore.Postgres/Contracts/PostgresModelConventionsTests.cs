using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Contracts;

[Trait("Category", "Unit")]
public sealed class PostgresModelConventionsTests : ModelConventionsContract
{
    protected override DbContextOptions<TestDbContext> Options => new DbContextOptionsBuilder<TestDbContext>().UseNpgsql("Host=localhost").Options;

    protected override string NotDeletedFilter => "\"deleted_at\" IS NULL";
}
