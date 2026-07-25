using FluentValidation;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateIpa;

public sealed class GenerateIpaQueryValidator : AbstractValidator<GenerateIpaQuery>
{
    public GenerateIpaQueryValidator()
    {
        RuleFor(x => x.Headword)
            .NotEmpty().WithMessage("Headword is required.")
            .MaximumLength(100).WithMessage("Headword cannot exceed 100 characters.");
    }
}
