using System.Runtime.CompilerServices;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Support;

internal static class SqlServerTestModel
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        TestModel.NotDeleted = index => index.WhereNotDeleted();
    }
}
