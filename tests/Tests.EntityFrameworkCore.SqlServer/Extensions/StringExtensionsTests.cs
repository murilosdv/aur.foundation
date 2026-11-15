using Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.SqlServer.Extensions;

[Trait("Feature", "LIKE patterns")]
[Trait("Category", "Unit")]
public sealed class StringExtensionsTests
{
    public static TheoryData<string, string> Patterns => new()
    {
        { "plain", "plain" },
        { "50%", "50[%]" },
        { "a_b", "a[_]b" },
        { "[tag]", "[[]tag]" },
        { "100%_[x]", "100[%][_][[]x]" },
    };

    [Theory(DisplayName = "The characters that mean something in a LIKE pattern are escaped to be matched literally")]
    [MemberData(nameof(Patterns))]
    public void WildcardsAreEscaped(string text, string expected)
    {
        // Arrange & Act
        var escaped = text.EscapeLikePattern();

        // Assert
        Assert.Equal(expected, escaped);
    }
}
