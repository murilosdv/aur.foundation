using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Constants;

/// <summary>
/// The sizes of text fields, in multiples of 8, named by size and not by what they hold. An application gives them a
/// meaning in its own rules (<c>TitleMaxLength = Large</c>) and uses that one value in the validator and in the column.
/// Inherit from it to add the sizes an application needs.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class FieldLengths
{
    /// <summary>16: codes, statuses.</summary>
    public const int Tiny = 16;

    /// <summary>32: handles, short names.</summary>
    public const int Small = 32;

    /// <summary>64: names.</summary>
    public const int Medium = 64;

    /// <summary>128: titles, subjects.</summary>
    public const int Large = 128;

    /// <summary>256: long titles, addresses, URLs.</summary>
    public const int XLarge = 256;

    /// <summary>512: descriptions.</summary>
    public const int XXLarge = 512;

    /// <summary>2048: comments and short texts.</summary>
    public const int Text = 2048;

    /// <summary>32768: the body of a document.</summary>
    public const int LongText = 32768;

    /// <summary>Lets an application inherit the constants; there is nothing to create.</summary>
    protected FieldLengths()
    {
    }
}
