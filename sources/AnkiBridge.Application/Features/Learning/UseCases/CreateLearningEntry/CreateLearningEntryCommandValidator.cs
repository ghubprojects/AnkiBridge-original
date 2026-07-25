using AnkiBridge.Domain.Enums;
using FluentValidation;

namespace AnkiBridge.Application.Features.Learning.UseCases.CreateLearningEntry;

public sealed class CreateLearningEntryCommandValidator : AbstractValidator<CreateLearningEntryCommand>
{
    public CreateLearningEntryCommandValidator()
    {
        // ── CORE TEXT PROPERTIES VALIDATION ─────────────────────────────────
        RuleFor(x => x.Headword)
            .NotEmpty().WithMessage("Headword is required.")
            .MaximumLength(50).WithMessage("Headword cannot exceed 50 characters.");

        RuleFor(x => x.PartOfSpeech)
            .IsInEnum().WithMessage("Invalid Part of Speech.");

        RuleFor(x => x.Accent)
            .IsInEnum().WithMessage("Invalid Accent.");

        RuleFor(x => x.Ipa)
            .NotEmpty().WithMessage("IPA pronunciation is required.")
            .MaximumLength(50).WithMessage("IPA cannot exceed 50 characters.");

        RuleFor(x => x.Cloze)
            .NotEmpty().WithMessage("Cloze context is required.")
            .MaximumLength(50).WithMessage("Cloze cannot exceed 50 characters.");

        RuleFor(x => x.Definition)
            .NotEmpty().WithMessage("Definition is required.")
            .MaximumLength(500).WithMessage("Definition cannot exceed 500 characters.");

        RuleFor(x => x.TranslationSource)
            .IsInEnum().WithMessage("Invalid Translation Source.");

        RuleFor(x => x.Translation)
            .NotEmpty().WithMessage("Translation is required.")
            .MaximumLength(100).WithMessage("Translation cannot exceed 100 characters.");

        // ── EXAMPLES VALIDATION ─────────────────────────────────────────────
        RuleFor(x => x.Examples)
            .NotNull().WithMessage("Examples list cannot be null.")
            .Must(x => x.Count() <= 3).WithMessage("You can provide a maximum of 3 examples."); // Đồng bộ với UI Form (Example1, 2, 3)

        RuleForEach(x => x.Examples)
            .NotEmpty().WithMessage("Example content cannot be empty.")
            .MaximumLength(200).WithMessage("Each example cannot exceed 200 characters.");

        // ── AUDIO OUTBOX DATA VALIDATION ────────────────────────────────────
        RuleFor(x => x.AudioSource)
            .IsInEnum().When(x => x.AudioSource.HasValue).WithMessage("Invalid Audio Source.");

        RuleFor(x => x.AudioSourceUrl)
            .NotEmpty().WithMessage("Audio source URL is required.")
            .Must(BeHttpUrl).WithMessage("Audio source URL must use HTTP or HTTPS.")
            .When(x => x.AudioSource.HasValue && x.AudioSource != AudioSource.User);

        RuleFor(x => x.AudioAbsolutePath)
            .NotEmpty().WithMessage("Audio local path is required for upload.")
            .Must(path => path is not null && Path.IsPathFullyQualified(path))
            .WithMessage("Audio local path must be absolute.")
            .When(x => x.AudioSource == AudioSource.User);

        RuleFor(x => x.AudioFileName)
            .NotEmpty().WithMessage("Audio file name is required.")
            .MaximumLength(255).WithMessage("Audio file name is too long.")
            .When(x => x.AudioSource == AudioSource.User);

        RuleFor(x => x.AudioContentType)
            .MaximumLength(100).WithMessage("Audio content type is too long.");

        // ── IMAGE OUTBOX DATA VALIDATION ────────────────────────────────────
        RuleFor(x => x.ImageSource)
            .IsInEnum().When(x => x.ImageSource.HasValue).WithMessage("Invalid Image Source.");

        RuleFor(x => x.ImageSourceUrl)
            .NotEmpty().WithMessage("Image source URL is required.")
            .Must(BeHttpUrl).WithMessage("Image source URL must use HTTP or HTTPS.")
            .When(x => x.ImageSource.HasValue && x.ImageSource != ImageSource.User);

        RuleFor(x => x.ImageAbsolutePath)
            .NotEmpty().WithMessage("Image local path is required for upload.")
            .Must(path => path is not null && Path.IsPathFullyQualified(path))
            .WithMessage("Image local path must be absolute.")
            .When(x => x.ImageSource == ImageSource.User);

        RuleFor(x => x.ImageFileName)
            .NotEmpty().WithMessage("Image file name is required.")
            .MaximumLength(255).WithMessage("Image file name is too long.")
            .When(x => x.ImageSource == ImageSource.User);

        RuleFor(x => x.ImageContentType)
            .MaximumLength(100).WithMessage("Image content type is too long.");
    }

    private static bool BeHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
