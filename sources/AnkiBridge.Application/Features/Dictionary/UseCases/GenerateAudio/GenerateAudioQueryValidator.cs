using FluentValidation;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateAudio;

public sealed class GenerateAudioQueryValidator : AbstractValidator<GenerateAudioQuery>
{
    public GenerateAudioQueryValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Headword is required.")
            .MaximumLength(100).WithMessage("Headword cannot exceed 100 characters.");
    }
}
