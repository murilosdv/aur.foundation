using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Support;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Contracts;

[Trait("Category", "Integration")]
public sealed class SqlServerDbContextTests(SqlServerFixture database) : DbContextContract<SqlServerFixture>(database);
