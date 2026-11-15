using Aurore.Foundation.EntityFrameworkCore.SqlServer.Models;
using Microsoft.Data.SqlClient;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;

/// <summary>
/// Builds SQL Server connection strings with the provider's keyword validation.
/// </summary>
public static class SqlServerOptionsExtensions
{
    /// <summary>
    /// Builds the connection string from SqlClient keywords and values.
    /// </summary>
    /// <param name="options">The connection options.</param>
    public static string ToConnectionString(this SqlServerOptions options)
    {
        return options.ToBuilder().ConnectionString;
    }

    internal static SqlConnectionStringBuilder ToBuilder(this SqlServerOptions options)
    {
        var builder = new SqlConnectionStringBuilder();
        foreach (var (keyword, value) in options)
            builder[keyword] = value;

        return builder;
    }
}
