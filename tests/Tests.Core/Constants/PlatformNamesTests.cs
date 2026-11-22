using System;
using Aurore.Foundation.Core.Constants;

namespace Aurore.Foundation.Tests.Core.Constants;

[CollectionDefinition(nameof(PlatformNamesTests), DisableParallelization = true)]
public sealed class PlatformNamesCollection;

[Collection(nameof(PlatformNamesTests))]
[Trait("Feature", "Platform names")]
public sealed class PlatformNamesTests
{
    public static TheoryData<string> InvalidPrefixes => ["", "Acme", "1acme", "ac-me", "ac me"];

    [Fact(DisplayName = "Every name is built from the prefix, aur by default")]
    public void NamesFollowThePrefix()
    {
        // Arrange & Act
        var names = new[] { PlatformNames.Telemetry.ServiceDomain, PlatformNames.Telemetry.CorrelationId, PlatformNames.Telemetry.ErrorCode, PlatformNames.HealthChecks.ClientHeader };

        // Assert
        Assert.Equal(["aur.service.domain", "aur.correlation.id", "aur.error.code", "AUR-HealthCheck-Client"], names);
    }

    [Fact(DisplayName = "Changing the prefix changes every name, and the header is written in upper case")]
    public void UsePrefixChangesEveryName()
    {
        try
        {
            // Arrange & Act
            PlatformNames.UsePrefix("acme1");

            // Assert
            Assert.Equal("acme1.feature.name", PlatformNames.Telemetry.FeatureName);
            Assert.Equal("ACME1-HealthCheck-Client", PlatformNames.HealthChecks.ClientHeader);
        }
        finally
        {
            PlatformNames.UsePrefix("aur");
        }
    }

    [Theory(DisplayName = "A prefix that is empty or not lower case letters and digits starting with a letter is refused")]
    [MemberData(nameof(InvalidPrefixes))]
    public void InvalidPrefixIsRefused(string prefix)
    {
        // Arrange & Act
        var act = () => PlatformNames.UsePrefix(prefix);

        // Assert
        Assert.Throws<ArgumentException>(act);
        Assert.Equal("aur", PlatformNames.Prefix);
    }
}
