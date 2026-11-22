using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Aurore.Foundation.Core.RequestPipeline.Models;
using FluentValidation;

namespace Aurore.Foundation.Core.RequestPipeline.Validators;

/// <summary>
/// Checks the request pipeline settings at startup, naming the configuration key of every problem.
/// </summary>
public sealed class RequestPipelineOptionsValidator : AbstractValidator<RequestPipelineOptions>
{
    private const int SecretLengthBytes = 32;

    /// <summary>
    /// Creates the validator.
    /// </summary>
    public RequestPipelineOptionsValidator()
    {
        RuleFor(x => x.ForwardedHeaders.KnownProxies).Custom(ValidateKnownProxies);
        RuleFor(x => x.ForwardedHeaders.KnownNetworks).Custom(ValidateKnownNetworks);

        RuleFor(x => x.KestrelLimits.MaxConcurrentConnections).GreaterThan(0L);
        RuleFor(x => x.KestrelLimits.MaxRequestHeaderCount).GreaterThan(0);
        RuleFor(x => x.KestrelLimits.RequestHeadersTimeoutSeconds).GreaterThan(0);
        RuleFor(x => x.KestrelLimits.KeepAliveTimeoutSeconds).GreaterThan(0);
        RuleFor(x => x.KestrelLimits.MaxRequestBodySizeBytes).GreaterThan(0L);

        RuleFor(x => x.Idempotency.CacheDurationSeconds).GreaterThan(0);

        RuleFor(x => x.Timeouts.DefaultSeconds).GreaterThan(0);

        // Dictionary entries are reported with the configuration key (RequestPipeline:RateLimiting:Policies:tests:PermitLimit).
        RuleFor(x => x.Timeouts.Policies).Custom(ValidateTimeoutPolicies);
        RuleFor(x => x.RateLimiting.Policies).Custom(ValidateRateLimitingPolicies);
        RuleFor(x => x.OutputCache.Policies).Custom(ValidateOutputCachePolicies);
        RuleFor(x => x.Privacy).Custom(ValidatePrivacy);
        RuleFor(x => x.CorsPolicies).Custom(ValidateCorsPolicies);
    }

    private static void ValidateKnownProxies(IEnumerable<string> proxies, ValidationContext<RequestPipelineOptions> context)
    {
        foreach (var proxy in proxies.Where(x => IPAddress.TryParse(x, out _) is false))
        {
            context.AddFailure($"'{proxy}' is not a valid IP address.");
        }
    }

    private static void ValidateKnownNetworks(IEnumerable<string> networks, ValidationContext<RequestPipelineOptions> context)
    {
        foreach (var network in networks.Where(x => IPNetwork.TryParse(x, out _) is false))
        {
            context.AddFailure($"'{network}' is not a valid network. Use CIDR notation, e.g. '172.18.0.0/16'.");
        }
    }

    private static void ValidateTimeoutPolicies(IDictionary<string, int> policies, ValidationContext<RequestPipelineOptions> context)
    {
        foreach (var (name, seconds) in policies)
        {
            if (seconds <= 0)
            {
                context.AddFailure($"Timeouts:Policies:{name}", $"'{name}' must be greater than zero, but was {seconds}.");
            }
        }
    }

    private static void ValidateRateLimitingPolicies(IDictionary<string, RateLimitPolicyOptions> policies, ValidationContext<RequestPipelineOptions> context)
    {
        foreach (var (name, policy) in policies)
        {
            AddIfNotPositive(context, $"RateLimiting:Policies:{name}:PermitLimit", policy.PermitLimit);
            AddIfNotPositive(context, $"RateLimiting:Policies:{name}:WindowSeconds", policy.WindowSeconds);
        }
    }

    private static void ValidateOutputCachePolicies(IDictionary<string, OutputCachePolicyOptions> policies, ValidationContext<RequestPipelineOptions> context)
    {
        foreach (var (name, policy) in policies)
        {
            AddIfNotPositive(context, $"OutputCache:Policies:{name}:ExpirationSeconds", policy.ExpirationSeconds);
        }
    }

    private static void ValidatePrivacy(PrivacyOptions privacy, ValidationContext<RequestPipelineOptions> context)
    {
        if (privacy.Enabled is false)
            return;

        ValidateKey(context, "Privacy:Keys:Current", privacy.Keys.Current);

        for (var i = 0; i < privacy.Keys.Previous.Length; i++)
        {
            ValidateKey(context, $"Privacy:Keys:Previous:{i}", privacy.Keys.Previous[i]);
        }
    }

    private static void ValidateCorsPolicies(IReadOnlyDictionary<string, CorsPolicyOptions> policies, ValidationContext<RequestPipelineOptions> context)
    {
        foreach (var (name, policy) in policies)
        {
            if (policy.PreflightMaxAgeSeconds < 0)
            {
                context.AddFailure($"CorsPolicies:{name}:PreflightMaxAgeSeconds", "Must not be negative.");
            }
        }
    }

    private static void ValidateKey(ValidationContext<RequestPipelineOptions> context, string path, string secret)
    {
        var buffer = new byte[SecretLengthBytes];

        if (Convert.TryFromBase64String(secret, buffer, out var written) is false || written != SecretLengthBytes)
        {
            context.AddFailure(path, $"Must be {SecretLengthBytes} random bytes, base64 encoded (set it through an environment variable).");
        }
    }

    private static void AddIfNotPositive(ValidationContext<RequestPipelineOptions> context, string key, int value)
    {
        if (value <= 0)
        {
            context.AddFailure(key, $"Must be greater than zero, but was {value}.");
        }
    }
}
