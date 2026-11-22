using Aurore.Foundation.Core.Validation;
using FluentValidation;

namespace Aurore.Foundation.Tests.Core.Validation;

[Trait("Feature", "Validation")]
public sealed class ValidatorVarianceTests
{
    [Fact(DisplayName = "A request validator of a base type validates the types derived from it")]
    public void RequestValidatorOfABaseTypeServesDerivedTypes()
    {
        // Arrange
        IRequestValidator<Animal> forAnimals = new AnimalRequestValidator();
        IRequestValidator<Dog> forDogs = forAnimals;

        // Act
        var result = forDogs.Validate(new Dog(""));

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "A feature validator of a base type validates the types derived from it")]
    public void FeatureValidatorOfABaseTypeServesDerivedTypes()
    {
        // Arrange
        IFeatureValidator<Animal> forAnimals = new AnimalFeatureValidator();
        IFeatureValidator<Dog> forDogs = forAnimals;

        // Act
        var result = forDogs.Validate(new Dog("Rex"));

        // Assert
        Assert.True(result.IsValid);
    }

    private class Animal(string name)
    {
        public string Name { get; } = name;
    }

    private sealed class Dog(string name) : Animal(name);

    private sealed class AnimalRequestValidator : RequestValidator<Animal>
    {
        public AnimalRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    private sealed class AnimalFeatureValidator : FeatureValidator<Animal>
    {
        public AnimalFeatureValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
