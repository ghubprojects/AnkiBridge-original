using FluentValidation;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.LookupTranslations;

public sealed class LookupTranslationsQueryValidator : AbstractValidator<LookupTranslationsQuery>
{
    public LookupTranslationsQueryValidator()
    {
        RuleFor(x => x.Headword)
            .NotEmpty().WithMessage("Headword is required.")
            .MaximumLength(100).WithMessage("Headword cannot exceed 100 characters.");
    }
}
