namespace AnkiBridge.Infrastructure.ExternalServices.Dictionary.Cambridge;

internal static class CambridgeDictionarySelectors
{
    private const string LearnersDictionary = "cald4"; // Cambridge Advanced Learner's Dictionary
    private const string AcademicDictionary = "cacd";  // Cambridge Academic Content Dictionary
    private const string EnglishVietnamese = "cenv";   // English-Vietnamese Dictionary

    /// <summary>English entry blocks only (no idioms) - used for single-word IPA lookups.</summary>
    public static readonly string[] EnglishEntryBlocks =
        EntryBlocksFor(LearnersDictionary, AcademicDictionary);

    /// <summary>English entry blocks plus idiom blocks - used for full definition lookups.</summary>
    public static readonly string[] EnglishEntryAndIdiomBlocks =
    [
        .. EntryBlocksFor(LearnersDictionary, AcademicDictionary),
        .. IdiomBlocksFor(LearnersDictionary, AcademicDictionary),
    ];

    /// <summary>English-Vietnamese entry and idiom blocks - used for translations.</summary>
    public static readonly string[] EnglishVietnameseEntryAndIdiomBlocks =
    [
        .. EntryBlocksFor(EnglishVietnamese),
        .. IdiomBlocksFor(EnglishVietnamese),
    ];

    public static readonly string[] Headword = [".dhw", ".hw"];
    public static readonly string[] PartOfSpeech = [".dpos", ".pos"];

    public static readonly string[] BritishPronunciationBlock = [".uk.dpron-i", ".uk"];
    public static readonly string[] AmericanPronunciationBlock = [".us.dpron-i", ".us"];
    public static readonly string[] Ipa = [".dpron .dipa", ".pron .ipa"];

    public static readonly string[] DefinitionBlock = [".dsense_b > .ddef_block", ".sense-body > .def-block"];
    public static readonly string[] Definition = [".ddef_d"];

    public static readonly string[] Example = [".deg", ".eg"];
    public static readonly string[] Translation = [".dtrans", ".trans"];

    private static string[] EntryBlocksFor(params string[] dictionaryIds) =>
        [.. dictionaryIds.Select(id => $".dictionary[data-id='{id}'] .entry-body__el")];

    private static string[] IdiomBlocksFor(params string[] dictionaryIds) =>
        [.. dictionaryIds.Select(id => $".dictionary[data-id='{id}'] .idiom-block")];
}
