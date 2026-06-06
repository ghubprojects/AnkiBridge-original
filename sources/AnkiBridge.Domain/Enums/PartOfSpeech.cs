namespace AnkiBridge.Domain.Enums;

public enum PartOfSpeech
{
    // Core lexical categories
    Noun,
    Verb,
    Adjective,
    Adverb,

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

    // Multi-word expressions
    Collocation,
    Idiom,
    Phrase,

    // Word formation
    Prefix,
    Suffix,

    // Others
    Exclamation,
    
    Other
}