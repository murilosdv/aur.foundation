using System;
using System.Linq;

namespace Aurore.Foundation.Core.Constants;

/// <summary>
/// The names the foundation gives to what it adds to telemetry and to requests, built from one prefix (<c>aur</c> by default). They are
/// the contract with whoever reads the telemetry, so change the prefix only for a platform of your own, and do it once, before the application
/// builds anything: <c>PlatformNames.UsePrefix("acme")</c> as the first line of <c>Program.cs</c>. Inherit from it to add the names an application needs.
/// </summary>
public abstract class PlatformNames
{
    /// <summary>The prefix of every name, in lower case.</summary>
    public static string Prefix { get; private set; } = "aur";

    /// <summary>Names added to the spans, logs and metrics.</summary>
    public static class Telemetry
    {
        /// <summary>Attribute <c>aur.service.domain</c>.</summary>
        public static string ServiceDomain => $"{Prefix}.service.domain";

        /// <summary>Attribute <c>aur.service.boundary</c>.</summary>
        public static string ServiceBoundary => $"{Prefix}.service.boundary";

        /// <summary>Attribute <c>aur.service.maintainer</c>.</summary>
        public static string ServiceMaintainer => $"{Prefix}.service.maintainer";

        /// <summary>Attribute <c>aur.correlation.id</c>.</summary>
        public static string CorrelationId => $"{Prefix}.correlation.id";

        /// <summary>Attribute <c>aur.feature.name</c>: the feature (use case) that ran.</summary>
        public static string FeatureName => $"{Prefix}.feature.name";

        /// <summary>Attribute <c>aur.error.code</c>: the code of the error a feature answered with.</summary>
        public static string ErrorCode => $"{Prefix}.error.code";
    }

    /// <summary>Names used by the health check endpoints.</summary>
    public static class HealthChecks
    {
        /// <summary>Header <c>AUR-HealthCheck-Client</c>, which marks a request as coming from a health check client.</summary>
        public static string ClientHeader => $"{Prefix.ToUpperInvariant()}-HealthCheck-Client";
    }

    /// <summary>Changes the prefix of every name. Call it once, first thing in <c>Program.cs</c>.</summary>
    /// <param name="prefix">Lower case letters and digits, starting with a letter.</param>
    /// <exception cref="ArgumentException">The prefix is empty or has anything but lower case letters and digits.</exception>
    public static void UsePrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix) || char.IsAsciiLetterLower(prefix[0]) is false || prefix.All(x => char.IsAsciiLetterLower(x) || char.IsAsciiDigit(x)) is false)
            throw new ArgumentException("The prefix must be lower case letters and digits, starting with a letter.", nameof(prefix));

        Prefix = prefix;
    }

    /// <summary>Lets an application inherit the names; there is nothing to create.</summary>
    protected PlatformNames()
    {
    }
}
