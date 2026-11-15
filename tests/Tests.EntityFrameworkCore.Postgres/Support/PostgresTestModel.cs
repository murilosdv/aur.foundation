using System.Runtime.CompilerServices;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Support;

internal static class PostgresTestModel
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        TestModel.NotDeleted = index => index.WhereNotDeleted();
    }
}
