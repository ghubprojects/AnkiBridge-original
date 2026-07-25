using AnkiBridge.Web.Components.Abstractions;

using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Web.Features.Learning.Models;

public sealed class LearningEntryGridItem : IGridItem<Guid>
{
    public Guid Id { get; init; }
    public string Headword { get; init; } = default!;
    public string PartOfSpeech { get; init; } = default!;
    public string Ipa { get; init; } = default!;
    public string Translation { get; init; } = default!;
    public UploadStatus AudioUploadStatus { get; init; }
    public UploadStatus ImageUploadStatus { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}
