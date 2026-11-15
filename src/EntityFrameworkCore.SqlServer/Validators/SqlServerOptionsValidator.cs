using System;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Models;
using FluentValidation;
using Microsoft.Data.SqlClient;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Validators;

/// <summary>
/// Validates SqlClient keywords, server/database selection and production transport security.
/// </summary>
public sealed class SqlServerOptionsValidator : AbstractValidator<SqlServerOptions>
{
    /// <summary>
    /// Creates the validator without requiring password authentication for integrated or Entra identities.
    /// </summary>
    /// <param name="isProduction">Whether encrypted, certificate-validated connections are required.</param>
    public SqlServerOptionsValidator(bool isProduction)
    {
        RuleFor(options => options).Custom((options, context) =>
        {
            SqlConnectionStringBuilder builder;
            try
            {
                builder = options.ToBuilder();
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                context.AddFailure("SqlServer", "Invalid SQL Server connection keyword or value.");
                return;
            }

            if (string.IsNullOrWhiteSpace(builder.DataSource))
                context.AddFailure("Data Source", "SQL Server data source is required.");
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
                context.AddFailure("Initial Catalog", "SQL Server database is required.");
            if (isProduction && builder.Encrypt == SqlConnectionEncryptOption.Optional)
                context.AddFailure("Encrypt", "Production SQL Server connections must use encryption.");
            if (isProduction && builder.TrustServerCertificate)
                context.AddFailure("Trust Server Certificate", "Production SQL Server connections must validate the server certificate.");
        });
    }
}
