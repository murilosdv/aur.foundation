using System;
using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Extensions;

[Trait("Feature", "Column helpers")]
[Trait("Category", "Unit")]
public sealed class PropertyBuilderExtensionsTests
{
    [Fact(DisplayName = "A DateTimeOffset is stored as datetimeoffset, and a DateTime as datetime2")]
    public void TimestampColumnFollowsTheClrType()
    {
        // Arrange
        var builder = new ModelBuilder();
        var entity = builder.Entity<Event>();

        // Act
        var offset = entity.Property(x => x.At).AsTimestampColumn();
        var local = entity.Property(x => x.Local).AsTimestampColumn(hasTimezone: false);
        var optional = entity.Property(x => x.Optional).AsTimestampColumn();

        // Assert
        Assert.Equal("datetimeoffset", offset.Metadata.GetColumnType());
        Assert.Equal("datetime2", local.Metadata.GetColumnType());
        Assert.Equal("datetimeoffset", optional.Metadata.GetColumnType());
    }

    [Fact(DisplayName = "The other combination of type and time zone is refused")]
    public void MismatchedTypeIsRefused()
    {
        // Arrange
        var property = new ModelBuilder().Entity<Event>().Property(x => x.Local);

        // Act
        var act = () => property.AsTimestampColumn();

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    private sealed class Event
    {
        public int Id { get; set; }
        public DateTimeOffset At { get; set; }
        public DateTime Local { get; set; }
        public DateTimeOffset? Optional { get; set; }
    }
}
