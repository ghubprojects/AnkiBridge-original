using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Learning.UseCases.CreateLearningEntry;

public sealed record CreateLearningEntryCommand(
    Guid? DictionaryEntryId,
    string Headword,
    PartOfSpeech PartOfSpeech,
    string Cloze,
    string Definition,
    IEnumerable<string> Examples,
    TranslationSource TranslationSource,
    string Translation,
    Accent Accent,
    string Ipa,
    AudioSource? AudioSource,
    string? AudioRelativePath,
    string? AudioAbsolutePath,
    string? AudioFileName,
    string? AudioContentType,
    ImageSource? ImageSource,
    string? ImageRelativePath,
    string? ImageAbsolutePath,
    string? ImageFileName,
    string? ImageContentType
) : IRequest<Result<Guid>>;
