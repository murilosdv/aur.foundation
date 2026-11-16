using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Contracts;

[Trait("Category", "Unit")]
public sealed class SqlServerModelConventionsTests : ModelConventionsContract
{
    protected override DbContextOptions<TestDbContext> Options => new DbContextOptionsBuilder<TestDbContext>().UseSqlServer("Server=localhost").Options;

    protected override string NotDeletedFilter => "[deleted_at] IS NULL";
}
