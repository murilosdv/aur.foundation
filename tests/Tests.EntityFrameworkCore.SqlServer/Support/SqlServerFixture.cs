using System;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Features.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Configuration;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Configuration;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Constants;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Models;
using Aurore.Foundation.Tests.EntityFrameworkCore.Support;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Support;

/// <summary>
/// A database of its own on the SQL Server of the machine (127.0.0.1:1433), created for the test class and dropped afterwards.
/// The server is shared by every project, and nothing of the tests is left on it. Fails with a hint when the server is not there.
/// </summary>
public class SqlServerFixture : IAsyncLifetime, IDatabaseFixture
{
    private const string ServerHint = "The tests need a SQL Server on 127.0.0.1:1433 with the user 'sa' and the password 'SqlServer@123Dev' (see docs/instructions/testing.md).";

    private readonly string _database = $"aurore_{Guid.NewGuid():N}";

    /// <summary>
    /// The settings of the database created for this fixture.
    /// </summary>
    public SqlServerOptions Options { get; } = new();

    /// <summary>
    /// The settings of a server that is not there.
    /// </summary>
    public static SqlServerOptions Unreachable()
    {
        var options = Server("master");
        options["Data Source"] = "127.0.0.1,55998";
        options["Connect Timeout"] = "1";

        return options;
    }

    private static SqlServerOptions Server(string database)
    {
        return new SqlServerOptions
        {
            ["Data Source"] = (Environment.GetEnvironmentVariable("TEST_SQLSERVER_HOST") ?? "127.0.0.1") + ",1433", // not "localhost": it resolves to ::1 first, and docker in WSL does not forward IPv6
            ["Initial Catalog"] = database,
            ["User ID"] = "sa",
            ["Password"] = "SqlServer@123Dev",
            ["Encrypt"] = "True",
            ["Trust Server Certificate"] = "True",
            ["Connect Timeout"] = "10",
        };
    }

    public async ValueTask InitializeAsync()
    {
        foreach (var (key, value) in Server(_database))
            Options[key] = value;

        try
        {
            await using var connection = new SqlConnection(Server("master").ToConnectionString());
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var create = new SqlCommand($"CREATE DATABASE [{_database}]", connection);
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Could not reach the test SQL Server. {ServerHint}", ex);
        }

        await using var provider = Provider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await SeedAsync(scope.ServiceProvider.GetRequiredService<TestDbContext>());
    }

    /// <summary>
    /// Adds the rows every test of the class starts from. Nothing by default.
    /// </summary>
    protected virtual ValueTask SeedAsync(TestDbContext db)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(Server("master").ToConnectionString());
        await connection.OpenAsync();
        await using var drop = new SqlCommand($"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE IF EXISTS [{_database}]", connection);
        await drop.ExecuteNonQueryAsync();
    }

    public bool IsUniqueViolation(Exception exception)
    {
        return exception is SqlException { Number: SqlServerErrorNumbers.UniqueIndexViolation or SqlServerErrorNumbers.UniqueConstraintViolation };
    }

    /// <summary>
    /// A container with the DbContext of the foundation (interceptors included) on this database, a time that tests can
    /// move, and the sample features. Dispose it when done.
    /// </summary>
    public ServiceProvider Provider(Action<FeaturesBuilder>? features = null, Action<IServiceCollection>? register = null, FakeTimeProvider? time = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(time ?? new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero)))
            .AddStandardDbContext<TestDbContext>(Options)
            .AddFeaturesFrom<CreatePerson>(features);

        register?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
