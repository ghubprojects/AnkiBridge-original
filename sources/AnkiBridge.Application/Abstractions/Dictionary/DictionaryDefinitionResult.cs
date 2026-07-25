namespace AnkiBridge.Application.Abstractions.Dictionary;

public sealed record DictionaryDefinitionResult(
    string Text,
    IReadOnlyList<string> Examples);
