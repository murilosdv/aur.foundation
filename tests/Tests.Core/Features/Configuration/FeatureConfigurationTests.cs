using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Features.Attributes;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Tests.Core.Features.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.Core.Features.Configuration;

[Trait("Feature", "Feature registration")]
public sealed class FeatureConfigurationTests
{
    [Fact(DisplayName = "A feature built by hand runs its logic alone, with no pipeline")]
    public async Task FeatureBuiltByHandRunsAlone()
    {
        // Arrange
        var trace = new Trace();
        var feature = new PlainFeature(trace);

        // Act
        var result = await feature.HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal("hello ana", result.Value);
        Assert.Equal(["feature"], trace.Steps);
    }

    [Fact(DisplayName = "Nothing is enabled unless asked for, so a plain feature runs alone")]
    public async Task NothingIsEnabledByDefault()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();
        var feature = provider.GetRequiredService<PlainFeature>();

        // Act
        await feature.HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A decorator enabled for all runs around every feature")]
    public async Task DecoratorEnabledForAllRunsAroundEveryFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.With<FirstDecorator>());

        // Act
        await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["first:before", "feature", "first:after"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A feature can enable a decorator for itself with the With attribute")]
    public async Task WithAttributeEnablesADecoratorForOneFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();

        // Act
        await provider.GetRequiredService<FirstFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["first:before", "feature", "first:after"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A feature can switch off a decorator enabled for all with the Without attribute")]
    public async Task WithoutAttributeSwitchesADecoratorOff()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.With<FirstDecorator>());

        // Act
        await provider.GetRequiredService<QuietFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "When a feature both enables and disables a decorator, disabling wins")]
    public async Task WithoutWinsOverWith()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();

        // Act
        await provider.GetRequiredService<ContradictoryFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "Decorators run by their order, not by the order they were enabled in, and a custom one defaults to last")]
    public async Task DecoratorsRunByTheirOrder()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features
            .With<DefaultOrderDecorator>()
            .With<SecondDecorator>()
            .With<FirstDecorator>());

        // Act
        await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(
            ["first:before", "second:before", "default:before", "feature", "second:after", "first:after"],
            provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A decorator that refuses stops the feature and its failure comes back as the feature's own result type")]
    public async Task RefusingDecoratorStopsTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.With<RefusingDecorator>());

        // Act
        var withValue = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);
        var command = await provider.GetRequiredService<CommandFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Same(RefusingDecorator.Refused, withValue.Error);
        Assert.Same(RefusingDecorator.Refused, command.Error);
        Assert.Empty(provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "Features are registered as themselves, once per scope, and classes that are not features are left out")]
    public void FeaturesAreRegisteredAsThemselvesPerScope()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        // Act
        var a = first.ServiceProvider.GetRequiredService<PlainFeature>();
        var b = first.ServiceProvider.GetRequiredService<PlainFeature>();
        var c = second.ServiceProvider.GetRequiredService<PlainFeature>();

        // Assert
        Assert.Same(a, b);
        Assert.NotSame(a, c);
        Assert.Null(provider.GetService<AbstractFeature>());
        Assert.Null(provider.GetService<NotAFeature>());
    }
}

public abstract class AbstractFeature : Feature<Ping, string>;

public sealed class CommandFeature(Trace trace) : Feature<Ping>
{
    protected override ValueTask<Result> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result.Success);
    }
}

[With<FirstDecorator>]
[Without<FirstDecorator>]
public sealed class ContradictoryFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}

/// <summary>
/// A custom decorator that does not set an Order, so it takes the default and runs last.
/// </summary>
public sealed class DefaultOrderDecorator(Trace trace) : IFeatureDecorator
{
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        trace.Steps.Add("default:before");

        return await next();
    }
}

public sealed class FirstDecorator(Trace trace) : IFeatureDecorator
{
    public int Order => 100;

    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        trace.Steps.Add("first:before");
        var result = await next();
        trace.Steps.Add("first:after");

        return result;
    }
}

[With<FirstDecorator>]
public sealed class FirstFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}

public abstract class NotAFeature;

[Without<FirstDecorator>]
public sealed class QuietFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}

/// <summary>
/// Answers on its own, without calling the feature.
/// </summary>
public sealed class RefusingDecorator : IFeatureDecorator
{
    public static readonly ApplicationError Refused = ApplicationError.Forbidden("test.refused", "Refused.");

    public ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        return ValueTask.FromResult<Result>(Refused);
    }
}

public sealed class SecondDecorator(Trace trace) : IFeatureDecorator
{
    public int Order => 200;

    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        trace.Steps.Add("second:before");
        var result = await next();
        trace.Steps.Add("second:after");

        return result;
    }
}
