using AnkiBridge.Domain.SeedWork;

namespace AnkiBridge.Domain.Aggregates.Flashcard.Notes.Events;

public sealed record NoteExportStartedDomainEvent(Guid AnkiNoteId) : DomainEvent;
