using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Features.Attributes;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Validation;
using Aurore.Foundation.Tests.Core.Features.Support;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Tests.Core.Features.Decorators;

[Trait("Feature", "Feature validation")]
public sealed class ValidationDecoratorTests
{
    [Fact(DisplayName = "A request with no feature validator goes straight to the feature")]
    public async Task NoValidatorsRunsTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.WithValidation());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "A failed business rule stops the feature and answers with the error the validator declares")]
    public async Task FailedRuleAnswersWithTheDeclaredError()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services.AddScoped<IFeatureValidator<Ping>, TakenNameValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);

        // Assert
        Assert.Equal(TakenNameValidator.Taken.Code, result.Error!.Code);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Contains("name", result.Error.Details.Keys);
        Assert.Empty(provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A validator that declares no error answers as a plain validation error")]
    public async Task ValidatorWithoutAnErrorAnswersAsValidation()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services.AddScoped<IFeatureValidator<Ping>, PlainBusinessValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("banned"), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Contains("name", result.Error.Details.Keys);
    }

    [Fact(DisplayName = "Which validators a request has is decided by each container, not once for the process")]
    public async Task ValidatorsAreResolvedPerContainer()
    {
        // Arrange
        using var withoutValidator = FeatureTestHost.Provider(features => features.WithValidation());
        using var withValidator = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services.AddScoped<IFeatureValidator<Ping>, TakenNameValidator>());

        // Act
        var first = await withoutValidator.GetRequiredService<PlainFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);
        var second = await withValidator.GetRequiredService<PlainFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);

        // Assert
        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
    }

    [Fact(DisplayName = "A request that passes every validator reaches the feature")]
    public async Task ValidRequestReachesTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services
                .AddScoped<IFeatureValidator<Ping>, TakenNameValidator>()
                .AddScoped<IFeatureValidator<Ping>, PlainBusinessValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "The validators of the request's shape are left to the HTTP filter, even for the same request type")]
    public async Task ShapeValidatorsAreNotRunByTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services
                .AddScoped<IRequestValidator<Ping>, ShapeValidator>()
                .AddScoped<IFeatureValidator<Ping>, PlainBusinessValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping(string.Empty), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "Validation can be enabled for one feature with the WithValidation attribute")]
    public async Task AttributeEnablesValidationForOneFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            register: services => services.AddScoped<IFeatureValidator<Ping>, TakenNameValidator>());

        // Act
        var validated = await provider.GetRequiredService<ValidatedFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);
        var plain = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);

        // Assert
        Assert.True(validated.IsFailure);
        Assert.True(plain.IsSuccess);
    }
}

public sealed class PlainBusinessValidator : FeatureValidator<Ping>
{
    public PlainBusinessValidator()
    {
        RuleFor(x => x.Name).NotEqual("banned").WithMessage("The name is banned.");
    }
}

public sealed class TakenNameValidator : FeatureValidator<Ping>
{
    public static readonly ApplicationError Taken = ApplicationError.Conflict("test.taken", "The name is taken.");

    public TakenNameValidator()
    {
        RuleFor(x => x.Name).NotEqual("taken").WithMessage("The name is taken.");
    }

    public override ApplicationError? Error => Taken;
}

[WithValidation]
public sealed class ValidatedFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}
