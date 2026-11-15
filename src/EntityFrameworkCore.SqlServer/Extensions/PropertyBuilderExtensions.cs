using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;

/// <summary>
/// SQL Server column configuration.
/// </summary>
public static class PropertyBuilderExtensions
{
    /// <summary>
    /// Maps DateTimeOffset to datetimeoffset or DateTime to datetime2; SQL Server timestamp is a rowversion, not a date.
    /// </summary>
    /// <typeparam name="TProperty">The property type, optionally nullable.</typeparam>
    /// <param name="propertyBuilder">The property being configured.</param>
    /// <param name="hasTimezone">True for DateTimeOffset; false for DateTime.</param>
    /// <exception cref="ArgumentException">The CLR type does not match the requested SQL type.</exception>
    public static PropertyBuilder<TProperty> AsTimestampColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, bool hasTimezone = true)
    {
        var type = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);
        if (type != (hasTimezone ? typeof(DateTimeOffset) : typeof(DateTime)))
            throw new ArgumentException("Use DateTimeOffset for datetimeoffset, or DateTime for datetime2.", nameof(hasTimezone));

        return propertyBuilder.HasColumnType(hasTimezone ? "datetimeoffset" : "datetime2");
    }
}
