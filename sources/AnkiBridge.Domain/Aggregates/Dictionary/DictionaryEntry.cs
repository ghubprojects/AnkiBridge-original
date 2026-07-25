using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public sealed class DictionaryEntry : AggregateRoot<Guid>
{
    public string Headword { get; private set; } = default!;
    public PartOfSpeech PartOfSpeech { get; private set; }
    public DictionarySource Source { get; private set; }

    private readonly List<DictionaryDefinition> _definitions = [];
    public IReadOnlyCollection<DictionaryDefinition> Definitions => _definitions.AsReadOnly();

    private readonly List<DictionaryTranslation> _translations = [];
    public IReadOnlyCollection<DictionaryTranslation> Translations => _translations.AsReadOnly();

    private readonly List<DictionaryPronunciation> _pronunciations = [];
    public IReadOnlyCollection<DictionaryPronunciation> Pronunciations => _pronunciations.AsReadOnly();

    private readonly List<DictionaryImage> _images = [];
    public IReadOnlyCollection<DictionaryImage> Images => _images.AsReadOnly();

    #region Constructors

    private DictionaryEntry() { }

    private DictionaryEntry(string headword, PartOfSpeech partOfSpeech, DictionarySource source)
    {
        Id = Guid.CreateVersion7();
        Headword = headword;
        PartOfSpeech = partOfSpeech;
        Source = source;
    }

    #endregion

    #region Factory Method

    public static Result<DictionaryEntry> Create(
        string headword,
        PartOfSpeech partOfSpeech,
        DictionarySource source)
    {
        if (string.IsNullOrWhiteSpace(headword))
            return Result.Failure<DictionaryEntry>("Headword must not be empty.");

        return new DictionaryEntry(headword.Trim(), partOfSpeech, source);
    }

    #endregion

    #region Behavior methods

    public Result AddPronunciation(
        string ipa,
        Accent accent,
        string? audioUrl,
        AudioSource? audioSource)
    {
        if (_pronunciations.Any(p => p.Accent == accent))
            return Result.Failure($"A pronunciation for accent '{accent}' already exists.");

        var result = DictionaryPronunciation.Create(ipa, accent, audioUrl, audioSource);
        if (result.IsFailure)
            return result;

        _pronunciations.Add(result.Value);

        return Result.Success();
    }
    public Result AddDefinition(string text, IReadOnlyList<string> examples)
    {
        var orderIndex = _definitions.Count + 1;

        var definitionResult = DictionaryDefinition.Create(text, orderIndex);
        if (definitionResult.IsFailure)
            return definitionResult;

        var definition = definitionResult.Value;

        var exampleResult = definition.AddExamples(examples);
        if (exampleResult.IsFailure)
            return exampleResult;

        _definitions.Add(definition);

        return Result.Success();
    }

    public Result AddImage(string url, ImageSource source)
    {
        var result = DictionaryImage.Create(url, source);
        if (result.IsFailure)
            return result;

        _images.Add(result.Value);

        return Result.Success();
    }

    public Result AddTranslation(string text, TranslationSource source)
    {
        var result = DictionaryTranslation.Create(text, source);
        if (result.IsFailure)
            return result;

        _translations.Add(result.Value);

        return Result.Success();
    }

    #endregion
}
