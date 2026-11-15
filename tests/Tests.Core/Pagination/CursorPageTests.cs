using System.Linq;
using Aurore.Foundation.Core.Pagination.Models;

namespace Aurore.Foundation.Tests.Core.Pagination;

[Trait("Feature", "Cursor page")]
public sealed class CursorPageTests
{
    [Fact(DisplayName = "Mapping a page changes the items and keeps the cursor and the size")]
    public void MapKeepsTheCursorAndTheSize()
    {
        // Arrange
        var page = new CursorPage<int> { Items = [1, 2, 3], NextCursor = "next", PageSize = 3 };

        // Act
        var mapped = page.Map(x => $"#{x}");

        // Assert
        Assert.Equal(["#1", "#2", "#3"], mapped.Items.ToArray());
        Assert.Equal("next", mapped.NextCursor);
        Assert.Equal(3, mapped.PageSize);
    }
}
