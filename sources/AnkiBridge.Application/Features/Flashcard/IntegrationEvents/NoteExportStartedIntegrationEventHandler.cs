using AnkiBridge.Application.Common.IntegrationEvents;
using AnkiBridge.Application.Features.Flashcard.Contracts.Anki;
using AnkiBridge.Application.Features.Flashcard.Contracts.Anki.Models;
using AnkiBridge.Domain.Aggregates.Flashcard.Notes;

namespace AnkiBridge.Application.Features.Flashcard.IntegrationEvents;

public sealed class NoteExportStartedIntegrationEventHandler(
    INoteRepository noteRepository,
    IAnkiService ankiService) 
    : IIntegrationEventHandler<NoteExportStartedIntegrationEvent>
{
    public async Task Handle(NoteExportStartedIntegrationEvent integrationEvent)
    {
        // The IntegrationEvent base already has an Id property; this event's note id is AnkiNoteId.
        var ankiNote = await noteRepository.GetByIdAsync(integrationEvent.AnkiNoteId, includeRelated: true);
        if (ankiNote is null)
        {
            return;
        }

        var exportDto = ToCreateDto(ankiNote);
        var exportResult = await ankiService.AddNotesAsync(new List<AnkiNote> { exportDto });
        if (exportResult.IsFailure)
        {
            ankiNote.MarkAsFailed();
            await noteRepository.UnitOfWork.SaveChangesAsync();
            return;
        }

        var externalId = exportResult.Value.FirstOrDefault();
        if (externalId <= 0)
            ankiNote.MarkAsFailed();
        else
            ankiNote.MarkAsSuccess(externalId);

        await noteRepository.UnitOfWork.SaveChangesAsync();
    }

    private static AnkiNote ToCreateDto(Note note)
    {
        var examples = note.LearningEntry.Examples
            .Select(x => x.Text)
            .ToArray();

        var example1 = examples.ElementAtOrDefault(0) ?? string.Empty;
        var example2 = examples.ElementAtOrDefault(1) ?? string.Empty;
        var example3 = examples.ElementAtOrDefault(2) ?? string.Empty;

        return new AnkiNote(
            note.NoteType.Name,
            note.Deck.Name,
            note.LearningEntry.Headword,
            note.LearningEntry.PartOfSpeech.ToString(),
            note.LearningEntry.Ipa,
            note.LearningEntry.Accent.ToString(),
            note.LearningEntry.Cloze,
            note.LearningEntry.Definition,
            note.LearningEntry.Translation,
            example1,
            example2,
            example3,
            note.LearningEntry.AudioPath ?? string.Empty,
            note.LearningEntry.ImagePath ?? string.Empty);
    }
}
