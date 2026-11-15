using System;
using System.Collections.Generic;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Models;

/// <summary>
/// Connection keywords understood by Microsoft.Data.SqlClient. Keep credentials outside committed settings.
/// </summary>
public sealed class SqlServerOptions : Dictionary<string, string>
{
    /// <summary>
    /// Creates case-insensitive connection options.
    /// </summary>
    public SqlServerOptions() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}
