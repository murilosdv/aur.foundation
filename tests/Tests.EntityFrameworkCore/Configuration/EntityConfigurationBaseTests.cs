using Aurore.Foundation.Core.Entities;
using Aurore.Foundation.EntityFrameworkCore.Configuration;

namespace Aurore.Foundation.Tests.EntityFrameworkCore.Configuration;

[Trait("Feature", "Entity configuration")]
public sealed class EntityConfigurationBaseTests
{
    public static TheoryData<Probe, string> TableNames => new()
    {
        { new Probe<Author>(), "Authors" },
        { new Probe<AuthorEntity>(), "Authors" },
        { new Probe<LegalEntityDocument>(), "LegalEntityDocuments" },
        { new Probe<Entity>(), "Entities" },
    };

    [Theory(DisplayName = "The table is the entity name in the plural, losing only a trailing Entity")]
    [MemberData(nameof(TableNames))]
    public void TableIsThePluralOfTheEntityName(Probe probe, string expected)
    {
        // Act
        var table = probe.Table;

        // Assert
        Assert.Equal(expected, table);
    }

    public abstract class Probe
    {
        public abstract string Table { get; }
    }

    public sealed class Probe<TEntity> : Probe
        where TEntity : Entity
    {
        public override string Table => new Config().Name;

        private sealed class Config : EntityConfigurationBase<TEntity>
        {
            public string Name => TableName;
        }
    }

    public sealed class Author : Entity;

    public sealed class AuthorEntity : Entity;

    public sealed class LegalEntityDocument : Entity;
}
