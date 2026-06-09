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

        // Nếu nguồn từ Dictionary -> Bắt buộc phải có đường dẫn tương đối (Relative Path) từ API trả về
        RuleFor(x => x.AudioRelativePath)
            .NotEmpty().WithMessage("Audio relative path is required when source is Dictionary.")
            .When(x => x.AudioSource == AudioSource.User);

        // Nếu nguồn từ User Upload -> Bắt buộc phải có đường dẫn tuyệt đối (Absolute Path) tới thư mục tạm trên Server
        RuleFor(x => x.AudioAbsolutePath)
            .NotEmpty().WithMessage("Audio file preview/temp path is required for upload.")
            .When(x => x.AudioSource == AudioSource.User);

        RuleFor(x => x.AudioFileName)
            .NotEmpty().WithMessage("Audio file name is required.")
            .MaximumLength(255).WithMessage("Audio file name is too long.")
            .When(x => x.AudioSource == AudioSource.User);

        // ── IMAGE OUTBOX DATA VALIDATION ────────────────────────────────────
        RuleFor(x => x.ImageSource)
            .IsInEnum().When(x => x.ImageSource.HasValue).WithMessage("Invalid Image Source.");

        // Nếu nguồn từ Dictionary -> Bắt buộc có Relative Path
        RuleFor(x => x.ImageRelativePath)
            .NotEmpty().WithMessage("Image relative path is required when source is Dictionary.")
            .When(x => x.ImageSource == ImageSource.User);

        // Nếu nguồn từ User Upload -> Bắt buộc có Absolute Path (đường dẫn file tạm) để Outbox Worker lấy đem đi upload thực sự
        RuleFor(x => x.ImageAbsolutePath)
            .NotEmpty().WithMessage("Image file preview/temp path is required for upload.")
            .When(x => x.ImageSource == ImageSource.User);

        RuleFor(x => x.ImageFileName)
            .NotEmpty().WithMessage("Image file name is required.")
            .MaximumLength(255).WithMessage("Image file name is too long.")
            .When(x => x.ImageSource == ImageSource.User);
    }
}