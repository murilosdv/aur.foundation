using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.EntityFrameworkCore.Extensions;

/// <summary>
/// Provider-independent relational column configuration.
/// </summary>
public static class PropertyBuilderExtensions
{
    /// <summary>
    /// Sets the precision and scale of a decimal column.
    /// </summary>
    /// <typeparam name="TProperty">The CLR type of the property.</typeparam>
    /// <param name="propertyBuilder">The property being configured.</param>
    /// <param name="precision">The total number of digits.</param>
    /// <param name="scale">The number of digits after the decimal point.</param>
    public static PropertyBuilder<TProperty> AsDecimalColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, int precision = 10, int scale = 2)
    {
        return propertyBuilder.HasPrecision(precision, scale);
    }
}
