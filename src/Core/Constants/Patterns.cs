using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Constants;

/// <summary>Regular expressions that applications share between validators. Inherit from it to add the ones an application needs.</summary>
[ExcludeFromCodeCoverage]
public abstract class Patterns
{
    /// <summary>Lowercase letters and digits in groups separated by single hyphens: <c>my-first-post</c>.</summary>
    public const string Slug = "^[a-z0-9]+(-[a-z0-9]+)*$";

    /// <summary>Lets an application inherit the constants; there is nothing to create.</summary>
    protected Patterns()
    {
    }
}
