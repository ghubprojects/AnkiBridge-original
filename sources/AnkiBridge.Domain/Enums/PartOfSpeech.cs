namespace AnkiBridge.Domain.Enums;

public enum PartOfSpeech
{
    // Core lexical categories
    Noun,
    Verb,
    Adjective,
    Adverb,

    // Multi-word expressions
    Collocation,
    Idiom,
    Phrase,

    // Function words
    Pronoun,
    Determiner,
    Preposition,
    Conjunction,

    // Verb types
    AuxiliaryVerb,
    ModalVerb,
    PhrasalVerb,

    // Numbers
    Number,
    OrdinalNumber,

    // Word formation
    Prefix,
    Suffix,

    // Others
    Exclamation,
    
    Other
}