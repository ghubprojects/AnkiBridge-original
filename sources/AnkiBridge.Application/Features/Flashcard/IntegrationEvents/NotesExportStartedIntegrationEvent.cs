using AnkiBridge.Application.Common.IntegrationEvents;

namespace AnkiBridge.Application.Features.Flashcard.IntegrationEvents;

public sealed record NotesExportStartedIntegrationEvent(IReadOnlyList<Guid> AnkiNoteIds) : IntegrationEvent;
