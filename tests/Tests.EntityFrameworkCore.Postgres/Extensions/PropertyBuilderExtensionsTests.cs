using System;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Postgres.Extensions;

[Trait("Feature", "Column helpers")]
[Trait("Category", "Unit")]
public sealed class PropertyBuilderExtensionsTests
{
    [Theory(DisplayName = "A timestamp column is stored with or without time zone")]
    [InlineData(true, "timestamptz")]
    [InlineData(false, "timestamp")]
    public void TimestampColumnFollowsTheTimeZone(bool hasTimezone, string expected)
    {
        // Arrange
        var builder = new ModelBuilder();
        var property = builder.Entity<Event>().Property(x => x.At);

        // Act
        property.AsTimestampColumn(hasTimezone);

        // Assert
        Assert.Equal(expected, property.Metadata.GetColumnType());
    }

    private sealed class Event
    {
        public int Id { get; set; }
        public DateTimeOffset At { get; set; }
    }
}
