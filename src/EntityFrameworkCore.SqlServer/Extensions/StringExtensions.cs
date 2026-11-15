namespace Aurore.Foundation.EntityFrameworkCore.SqlServer.Extensions;

/// <summary>
/// SQL Server query pattern helpers.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Escapes SQL Server LIKE metacharacters so the supplied text is matched literally.
    /// </summary>
    /// <param name="text">The literal text to include in a LIKE pattern.</param>
    public static string EscapeLikePattern(this string text)
    {
        return text.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
