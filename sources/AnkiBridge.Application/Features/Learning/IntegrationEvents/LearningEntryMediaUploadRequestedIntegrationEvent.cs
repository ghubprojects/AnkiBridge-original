using AnkiBridge.Application.Common.IntegrationEvents;

namespace AnkiBridge.Application.Features.Learning.IntegrationEvents;

public enum LearningEntryMediaKind
{
    Audio = 1,
    Image = 2,
}

public sealed record LearningEntryMediaUploadRequestedIntegrationEvent(
    Guid LearningEntryId,
    LearningEntryMediaKind MediaKind,
    string BlobName,
    string ContentType,
    string? SourceUrl,
    string? LocalFilePath) : IntegrationEvent;
