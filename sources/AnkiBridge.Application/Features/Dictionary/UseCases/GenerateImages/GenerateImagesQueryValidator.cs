using FluentValidation;

namespace AnkiBridge.Application.Features.Dictionary.UseCases.GenerateImages;

public sealed class GenerateImagesQueryValidator : AbstractValidator<GenerateImagesQuery>
{
    public GenerateImagesQueryValidator()
    {
        RuleFor(x => x.Keyword)
            .NotEmpty().WithMessage("Keyword is required.")
            .MaximumLength(100).WithMessage("Keyword cannot exceed 100 characters.");
    }
}
