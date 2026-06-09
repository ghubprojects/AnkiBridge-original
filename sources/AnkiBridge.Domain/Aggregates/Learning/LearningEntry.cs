using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Learning;

public sealed class LearningEntry : AggregateRoot<Guid>, IAuditableEntity, ISoftDeleteEntity
{
    public Guid? DictionaryEntryId { get; private set; }

    public string Headword { get; private set; } = default!;
    public PartOfSpeech PartOfSpeech { get; private set; }
    public string Cloze { get; private set; } = default!;
    public string Definition { get; private set; } = default!;

    private readonly List<LearningExample> _examples = [];
    public IReadOnlyCollection<LearningExample> Examples => _examples.AsReadOnly();

    public TranslationSource TranslationSource { get; private set; }
    public string Translation { get; private set; } = default!;

    public Accent Accent { get; private set; }
    public string Ipa { get; private set; } = default!;
    public AudioSource? AudioSource { get; private set; }
    public string? AudioPath { get; private set; }

    public ImageSource? ImageSource { get; private set; }
    public string? ImagePath { get; private set; }

    #region Audit
    public DateTimeOffset CreatedAt { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset? LastModifiedAt { get; }
    public Guid? LastModifiedBy { get; }
    #endregion

    #region Soft Delete
    public bool IsDeleted { get; }
    public DateTimeOffset? DeletedAt { get; }
    public Guid? DeletedBy { get; }
    #endregion

    private LearningEntry() { }

    private LearningEntry(
        Guid? dictionaryEntryId,
        string headword,
        PartOfSpeech partOfSpeech,
        string cloze,
        string definition,
        TranslationSource translationSource,
        string translation,
        Accent accent,
        string ipa,
        AudioSource? audioSource,
        string? audioPath,
        ImageSource? imageSource,
        string? imagePath)
    {
        Id = Guid.CreateVersion7();

        DictionaryEntryId = dictionaryEntryId;
        Headword = headword;
        PartOfSpeech = partOfSpeech;
        Cloze = cloze;
        Definition = definition;

        TranslationSource = translationSource;
        Translation = translation;

        Accent = accent;
        Ipa = ipa;
        AudioSource = audioSource;
        AudioPath = audioPath;

        ImageSource = imageSource;
        ImagePath = imagePath;
    }

    public static Result<LearningEntry> Create(
        Guid? dictionaryEntryId,
        string headword,
        PartOfSpeech partOfSpeech,
        string cloze,
        string definition,
        IEnumerable<string> examples,
        TranslationSource translationSource,
        string translation,
        Accent accent,
        string ipa,
        AudioSource? audioSource = null,
        string? audioPath = null,
        ImageSource? imageSource = null,
        string? imagePath = null)
    {
        if (string.IsNullOrWhiteSpace(headword)) return Result.Failure<LearningEntry>("Headword must not be empty.");
        if (string.IsNullOrWhiteSpace(cloze)) return Result.Failure<LearningEntry>("Cloze must not be empty.");
        if (string.IsNullOrWhiteSpace(definition)) return Result.Failure<LearningEntry>("Definition must not be empty.");
        if (string.IsNullOrWhiteSpace(translation)) return Result.Failure<LearningEntry>("Translation must not be empty.");

        var entry = new LearningEntry(
            dictionaryEntryId,
            headword.Trim(),
            partOfSpeech,
            cloze.Trim(),
            definition.Trim(),
            translationSource,
            translation.Trim(),
            accent,
            ipa.Trim(),
            audioSource,
            audioPath?.Trim(),
            imageSource,
            imagePath?.Trim());

        var newExamples = examples.Select(x => ((Guid?)null, x));

        var upsertResult = entry.UpsertExamples(newExamples);
        if (upsertResult.IsFailure)
            return upsertResult.ToFailure<LearningEntry>();

        return entry;
    }

    public void SetAudioFromDictionary(string? relativePath)
    {
        AudioSource = Enums.AudioSource.User;
        AudioPath = relativePath?.Trim();
    }

    public string PrepareUserAudioProcessing(string originalFileName)
    {
        AudioSource = Enums.AudioSource.User;
        // Đặt một path đích an toàn dựa trên Id của thực thể để lưu tạm vào DB
        AudioPath = $"learning-entries/{Id}/audios/{Guid.NewGuid()}_{originalFileName}";
        return AudioPath;
    }

    public void SetImageFromDictionary(string? relativePath)
    {
        ImageSource = Enums.ImageSource.User;
        ImagePath = relativePath?.Trim();
    }

    public string PrepareUserImageProcessing(string originalFileName)
    {
        ImageSource = Enums.ImageSource.User;
        // Đặt một path đích an toàn dựa trên Id của thực thể để lưu tạm vào DB
        ImagePath = $"learning-entries/{Id}/images/{Guid.NewGuid()}_{originalFileName}";
        return ImagePath;
    }

    public Result Update(
        Guid? dictionaryEntryId,
        string headword,
        PartOfSpeech partOfSpeech,
        string cloze,
        string definition,
        IEnumerable<(Guid? Id, string Text)> examples,
        TranslationSource translationSource,
        string translation,
        Accent accent,
        string ipa,
        AudioSource? audioSource = null,
        string? audioPath = null,
        ImageSource? imageSource = null,
        string? imagePath = null)
    {
        if (string.IsNullOrWhiteSpace(headword)) return Result.Failure("Headword must not be empty.");
        if (string.IsNullOrWhiteSpace(cloze)) return Result.Failure("Cloze must not be empty.");
        if (string.IsNullOrWhiteSpace(definition)) return Result.Failure("Definition must not be empty.");
        if (string.IsNullOrWhiteSpace(translation)) return Result.Failure("Translation must not be empty.");

        var upsertResult = UpsertExamples(examples);
        if (upsertResult.IsFailure)
            return upsertResult;

        DictionaryEntryId = dictionaryEntryId;
        Headword = headword.Trim();
        PartOfSpeech = partOfSpeech;
        Cloze = cloze.Trim();
        Definition = definition.Trim();

        TranslationSource = translationSource;
        Translation = translation.Trim();

        Accent = accent;
        Ipa = ipa.Trim();
        AudioSource = audioSource;
        AudioPath = audioPath?.Trim();

        ImageSource = imageSource;
        ImagePath = imagePath?.Trim();

        return Result.Success();
    }

    private Result UpsertExamples(IEnumerable<(Guid? Id, string Text)> examples)
    {
        var incoming = examples.ToList();

        var examplesById = incoming
            .Where(ne => ne.Id.HasValue)
            .ToDictionary(ne => ne.Id!.Value, ne => ne.Text);

        _examples.RemoveAll(e => !examplesById.ContainsKey(e.Id));

        foreach (var existing in _examples)
        {
            if (!examplesById.TryGetValue(existing.Id, out var newText))
                continue;

            var updateResult = existing.UpdateText(newText);
            if (updateResult.IsFailure)
                return updateResult;
        }

        foreach (var ne in incoming.Where(ne => !ne.Id.HasValue))
        {
            var createResult = LearningExample.Create(ne.Text);
            if (createResult.IsFailure)
                return Result.Failure(createResult.Error.Message);

            _examples.Add(createResult.Value);
        }

        return Result.Success();
    }
}