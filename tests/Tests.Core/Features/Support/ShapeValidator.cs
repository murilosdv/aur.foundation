using Aurore.Foundation.Core.Validation;
using FluentValidation;

namespace Aurore.Foundation.Tests.Core.Features.Support;

public sealed class ShapeValidator : RequestValidator<Ping>
{
    public ShapeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("The name is required.");
    }
}
