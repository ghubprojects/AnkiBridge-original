using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Features.Learning.Contracts.QueryServices.Models;

public sealed record LearningEntrySearchResult(
    Guid Id,
    string Headword,
    PartOfSpeech PartOfSpeech,
    string Ipa,
    string Translation,
    UploadStatus AudioUploadStatus,
    UploadStatus ImageUploadStatus,
    DateTimeOffset CreatedAt);
