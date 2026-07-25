namespace AnkiBridge.Application.Features.Dictionary.Contracts.QueryServices.Models;

public sealed record DictionaryEntryDetailDefinition(
    string Text,
    IReadOnlyList<string> Examples
);
