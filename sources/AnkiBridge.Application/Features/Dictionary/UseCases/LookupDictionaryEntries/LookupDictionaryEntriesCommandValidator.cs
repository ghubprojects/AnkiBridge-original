using FluentValidation;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.LookupDictionaryEntries;

public sealed class LookupDictionaryEntriesCommandValidator : AbstractValidator<LookupDictionaryEntriesCommand>
{
    public LookupDictionaryEntriesCommandValidator()
    {
        RuleFor(x => x.Headword)
            .NotEmpty().WithMessage("Headword is required.")
            .MaximumLength(100).WithMessage("Headword cannot exceed 100 characters.");
    }
}
