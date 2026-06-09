using AnkiBridge.Application.Common.IntegrationEvents;

namespace AnkiBridge.Application.Features.Flashcard.IntegrationEvents;

public sealed record NoteExportStartedIntegrationEvent(Guid AnkiNoteId) : IntegrationEvent;
