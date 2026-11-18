using Aurore.Foundation.Tests.EntityFrameworkCore.Contracts;
using Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Support;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Contracts;

[Trait("Category", "Integration")]
public sealed class PostgresTimestampTests(PostgresFixture database) : TimestampContract<PostgresFixture>(database);
