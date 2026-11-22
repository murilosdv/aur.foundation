using System;
using System.Collections.Generic;
using Aurore.Foundation.Core.Extensions;

namespace Aurore.Foundation.Tests.Core.Extensions;

[Trait("Feature", "String helpers")]
public sealed class StringExtensionsTests
{
    private static readonly Dictionary<string, Func<string?, string>> _conversions = new()
    {
        ["ToTitleCase"] = x => x.ToTitleCase(),
        ["ToSentenceCase"] = x => x.ToSentenceCase(),
        ["ToPascalCase"] = x => x.ToPascalCase(),
        ["ToCamelCase"] = x => x.ToCamelCase(),
        ["ToSnakeCase"] = x => x.ToSnakeCase(),
        ["ToSlugCase"] = x => x.ToSlugCase(),
        ["ToPlural"] = x => x.ToPlural(),
        ["ToSingular"] = x => x.ToSingular(),
        ["RemoveDiacritics"] = x => x.RemoveDiacritics(),
    };

    public static TheoryData<string> NullToEmpty => [.. _conversions.Keys];

    public static TheoryData<string?, string[]> Words => new()
    {
        { "BlogService", ["Blog", "Service"] },
        { "blogService", ["blog", "Service"] },
        { "HTTPServer", ["HTTP", "Server"] },
        { "Version2Api", ["Version2", "Api"] },
        { "ArchetypesAPI", ["Archetypes", "API"] },
        { "blog-service_name.v2", ["blog", "service", "name", "v2"] },
        { "  padded  text ", ["padded", "text"] },
        { "ação de usuário", ["ação", "de", "usuário"] },
        { "", [] },
        { "   ", [] },
        { null, [] },
    };

    public static TheoryData<string, string> SnakeCases => new()
    {
        { "BlogService", "blog_service" },
        { "PageSize2", "page_size2" },
        { "HTTPServer", "http_server" },
        { "Ação de Usuário", "acao_de_usuario" },
    };

    public static TheoryData<string, string> SlugCases => new()
    {
        { "BlogService", "blog-service" },
        { "ArchetypesAPI", "archetypes-api" },
        { "HTTPServer", "http-server" },
        { "Ação de Usuário", "acao-de-usuario" },
    };

    [Theory(DisplayName = "A null text becomes an empty string in every conversion")]
    [MemberData(nameof(NullToEmpty))]
    public void NullBecomesEmpty(string conversion)
    {
        // Arrange
        string? value = null;

        // Act
        var converted = _conversions[conversion](value);

        // Assert
        Assert.Equal(string.Empty, converted);
    }

    [Theory(DisplayName = "Words are split on separators and on case changes, acronyms included")]
    [MemberData(nameof(Words))]
    public void WordsAreSplit(string? value, string[] expected)
    {
        // Arrange
        // Act
        var words = value.SplitWords();

        // Assert
        Assert.Equal(expected, words);
    }

    [Theory(DisplayName = "Snake case drops accents and lowers the text, or upper cases it on request")]
    [MemberData(nameof(SnakeCases))]
    public void SnakeCaseDropsAccents(string value, string expected)
    {
        // Arrange
        // Act
        var lower = value.ToSnakeCase();
        var upper = value.ToSnakeCase(lowercase: false);

        // Assert
        Assert.Equal(expected, lower);
        Assert.Equal(expected.ToUpperInvariant(), upper);
    }

    [Theory(DisplayName = "Slug case drops accents and lowers the text, or upper cases it on request")]
    [MemberData(nameof(SlugCases))]
    public void SlugCaseDropsAccents(string value, string expected)
    {
        // Arrange
        // Act
        var lower = value.ToSlugCase();
        var upper = value.ToSlugCase(lowercase: false);

        // Assert
        Assert.Equal(expected, lower);
        Assert.Equal(expected.ToUpperInvariant(), upper);
    }

    [Fact(DisplayName = "Accents are removed and the letters stay")]
    public void AccentsAreRemoved()
    {
        // Arrange
        // Act
        var plain = "Ação, coração e café".RemoveDiacritics();

        // Assert
        Assert.Equal("Acao, coracao e cafe", plain);
    }
}
