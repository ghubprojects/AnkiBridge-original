using AnkiBridge.Domain.Aggregates.Flashcard.Notes.Events;
using AnkiBridge.Domain.Aggregates.Flashcard.Decks;
using AnkiBridge.Domain.Aggregates.Flashcard.NoteTypes;
using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Flashcard.Notes;

public sealed class Note : AggregateRoot<Guid>, IAuditableEntity, ISoftDeleteEntity
{
    public Guid LearningEntryId { get; private set; }
    public Guid NoteTypeId { get; private set; }
    public Guid DeckId { get; private set; }
    public ExportStatus Status { get; private set; }
    public long ExternalId { get; private set; }
    public DateTimeOffset? ExportedAt { get; private set; }

    public LearningEntry LearningEntry { get; private set; } = default!;
    public NoteType NoteType { get; private set; } = default!;
    public Deck Deck { get; private set; } = default!;

    #region Audit

    public DateTimeOffset CreatedAt { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset? LastModifiedAt { get; }
    public Guid? LastModifiedBy { get; }

    #endregion

    #region Soft Delete

    public bool IsDeleted { get; }
    public DateTimeOffset? DeletedAt { get; }
    public Guid? DeletedBy { get; }

    #endregion

    private Note() { }

    private Note(
        Guid learningEntryId,
        Guid noteTypeId,
        Guid deckId)
    {
        Id = Guid.CreateVersion7();
        LearningEntryId = learningEntryId;
        NoteTypeId = noteTypeId;
        DeckId = deckId;
        Status = ExportStatus.NotStarted;
    }

    public static Note Create(
        Guid learningEntryId,
        Guid noteTypeId,
        Guid deckId)
    {
        return new Note(
            learningEntryId,
            noteTypeId,
            deckId);
    }

    public Result MarkAsProcessing()
    {
        if (Status == ExportStatus.Success)
            return Result.Failure("Cannot mark as processing because the export has already succeeded.", ErrorType.Conflict);

        Status = ExportStatus.Processing;

        AddDomainEvent(new NoteExportStartedDomainEvent(Id));

        return Result.Success();
    }

    public Result MarkAsProcessingSilently()
    {
        if (Status == ExportStatus.Success)
            return Result.Failure("Cannot mark as processing because the export has already succeeded.", ErrorType.Conflict);

        Status = ExportStatus.Processing;
        return Result.Success();
    }

    public Result MarkAsSuccess(long externalId)
    {
        if (Status != ExportStatus.Processing)
            return Result.Failure("Cannot mark as success unless the item is being processed.", ErrorType.Conflict);

        Status = ExportStatus.Success;
        ExternalId = externalId;
        ExportedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public Result MarkAsFailed()
    {
        if (Status != ExportStatus.Processing)
            return Result.Failure("Cannot mark as failed unless the item is being processed.", ErrorType.Conflict);

        Status = ExportStatus.Failed;

        return Result.Success();
    }
}
