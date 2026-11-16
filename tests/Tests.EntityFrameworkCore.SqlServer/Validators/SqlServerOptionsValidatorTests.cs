using System.Collections.Generic;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Models;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Validators;
using Microsoft.Extensions.Configuration;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Validators;

[Trait("Feature", "SQL Server settings")]
[Trait("Category", "Unit")]
public sealed class SqlServerOptionsValidatorTests
{
    public static TheoryData<string> RequiredKeywords => new() { "Data Source", "Initial Catalog" };

    public static TheoryData<string, string> InvalidSettings => new()
    {
        { "Colour", "red" }, // not a keyword SqlClient knows
        { "Connect Timeout", "not-a-number" }, // a value of the wrong type
    };

    [Fact(DisplayName = "Complete settings are valid")]
    public void CompleteSettingsAreValid()
    {
        // Arrange
        var validator = new SqlServerOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(Complete());

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory(DisplayName = "A missing required keyword is reported by name")]
    [MemberData(nameof(RequiredKeywords))]
    public void MissingKeywordIsReported(string keyword)
    {
        // Arrange
        var validator = new SqlServerOptionsValidator(isProduction: false);
        var options = Complete();
        options.Remove(keyword);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == keyword);
    }

    [Theory(DisplayName = "An unknown keyword or a value of the wrong type is rejected")]
    [MemberData(nameof(InvalidSettings))]
    public void InvalidSettingsAreRejected(string keyword, string value)
    {
        // Arrange
        var validator = new SqlServerOptionsValidator(isProduction: false);
        var options = Complete();
        options[keyword] = value;

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory(DisplayName = "Production requires encryption and a validated certificate, any other environment accepts the development settings")]
    [InlineData(false, "True", "True", true)]
    [InlineData(true, "True", "True", false)] // the certificate is trusted without validation
    [InlineData(true, "Optional", "False", false)] // no encryption
    [InlineData(true, "True", "False", true)]
    public void ProductionRequiresEncryptionAndACertificate(
        bool isProduction,
        string encrypt,
        string trustCertificate,
        bool expectedValid)
    {
        // Arrange
        var validator = new SqlServerOptionsValidator(isProduction);
        var options = Complete();
        options["Encrypt"] = encrypt;
        options["Trust Server Certificate"] = trustCertificate;

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact(DisplayName = "Keywords are not case sensitive")]
    public void KeywordsAreNotCaseSensitive()
    {
        // Arrange
        var validator = new SqlServerOptionsValidator(isProduction: false);
        var options = new SqlServerOptions { ["data source"] = "localhost", ["INITIAL CATALOG"] = "app" };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "The settings bind from a configuration section as a free-form map, and make a connection string")]
    public void SettingsBindFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SqlServer:Data Source"] = "db",
                ["SqlServer:Initial Catalog"] = "app",
            })
            .Build();

        // Act
        var options = configuration.GetSection("SqlServer").Get<SqlServerOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("db", options["data source"]);
        Assert.Contains("Initial Catalog=app", options.ToConnectionString());
    }

    private static SqlServerOptions Complete()
    {
        return new SqlServerOptions { ["Data Source"] = "localhost", ["Initial Catalog"] = "app", ["User ID"] = "app", ["Password"] = "secret" };
    }
}
