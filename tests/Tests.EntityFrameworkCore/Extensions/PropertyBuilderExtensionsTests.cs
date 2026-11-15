using Aurore.Foundation.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Extensions;

[Trait("Feature", "Column helpers")]
public sealed class PropertyBuilderExtensionsTests
{
    [Fact(DisplayName = "A decimal column gets the precision and scale asked for, and 10 and 2 by default")]
    public void DecimalColumnHasPrecisionAndScale()
    {
        // Arrange
        var builder = new ModelBuilder();
        var invoice = builder.Entity<Invoice>();

        // Act
        invoice.Property(x => x.Total).AsDecimalColumn();
        invoice.Property(x => x.Rate).AsDecimalColumn(18, 6);

        // Assert
        var type = invoice.Metadata;
        Assert.Equal((10, 2), (type.FindProperty(nameof(Invoice.Total))!.GetPrecision(), type.FindProperty(nameof(Invoice.Total))!.GetScale()));
        Assert.Equal((18, 6), (type.FindProperty(nameof(Invoice.Rate))!.GetPrecision(), type.FindProperty(nameof(Invoice.Rate))!.GetScale()));
    }

    private sealed class Invoice
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
        public decimal Rate { get; set; }
    }
}
