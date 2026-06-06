using AnkiBridge.Application.Common.IntegrationEvents;
using AnkiBridge.Application.Features.Flashcard.Contracts.Anki;
using AnkiBridge.Application.Features.Flashcard.Contracts.Anki.Models;
using AnkiBridge.Domain.Aggregates.Flashcard.Notes;

namespace AnkiBridge.Application.Features.Flashcard.IntegrationEvents;

public sealed class AnkiNotesExportStartedIntegrationEventHandler(
    INoteRepository noteRepository,
    IAnkiService ankiService)
    : IIntegrationEventHandler<AnkiNotesExportStartedIntegrationEvent>
{
    public async Task Handle(AnkiNotesExportStartedIntegrationEvent integrationEvent)
    {
        var ids = integrationEvent.AnkiNoteIds.Distinct().ToArray();

        if (ids.Length == 0)
            return;

        var notes = await noteRepository.GetByIdsAsync(ids, includeRelated: true);

        // Guard: command handler already validated existence, but skip if inconsistent.
        if (notes.Count != ids.Length)
            return;

        // ── Step 1: push media files into Anki's media folder ──────────────────
        // Results are keyed by LearningEntry.Id so ToCreateDto can look them up.
        var mediaMap = await StoreMediaFilesAsync(notes);

        // ── Step 2: build note DTOs with resolved Anki media references ────────
        var createDtos = notes
            .Select(note => ToCreateDto(note, mediaMap.GetValueOrDefault(note.LearningEntry.Id)))
            .ToList();

        // ── Step 3: batch-create notes in Anki ─────────────────────────────────
        var exportResult = await ankiService.AddNotesAsync(createDtos);

        if (exportResult.IsFailure)
        {
            foreach (var note in notes)
                note.MarkAsFailed();

            await noteRepository.UnitOfWork.SaveChangesAsync();
            return;
        }

        // ── Step 4: update export status per note ──────────────────────────────
        // AnkiConnect returns IDs in the same order as the submitted notes array.
        var externalIds = exportResult.Value;

        for (var i = 0; i < notes.Count; i++)
        {
            var externalId = externalIds[i];

            // AnkiConnect returns 0 / null for a note that could not be added
            // (e.g. duplicate), so treat those as failures.
            if (externalId <= 0)
            {
                notes[i].MarkAsFailed();
                continue;
            }

            notes[i].MarkAsSuccess(externalId);
        }

        await noteRepository.UnitOfWork.SaveChangesAsync();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// For each note that has an AudioPath or ImagePath, downloads the file from
    /// Azure Blob Storage (emulator) into Anki's media folder via <c>storeMediaFile</c>.
    ///
    /// Filenames are deterministic:  ab_audio_{entryId}.mp3 / ab_image_{entryId}.jpg
    /// so repeated exports are idempotent (AnkiConnect overwrites by default).
    ///
    /// Media failures are silently swallowed — the note is still exported, just
    /// without audio/image.
    /// </summary>
    private async Task<Dictionary<Guid, MediaRefs>> StoreMediaFilesAsync(
        IReadOnlyList<Note> notes)
    {
        var result = new Dictionary<Guid, MediaRefs>(notes.Count);

        foreach (var note in notes)
        {
            var entry = note.LearningEntry;
            string? audioRef = null;
            string? imageRef = null;

            if (!string.IsNullOrEmpty(entry.AudioPath))
            {
                var ext = GetExtensionFromUrl(entry.AudioPath);     // e.g. ".mp3"
                var filename = $"ab_audio_{entry.Id:N}{ext}";            // e.g. "ab_audio_<guid>.mp3"

                var storeResult = await ankiService.StoreMediaFileFromUrlAsync(filename, entry.AudioPath);

                if (storeResult.IsSuccess)
                {
                    // Anki field value that makes the card play audio on reveal
                    audioRef = $"[sound:{storeResult.Value}]";
                }
            }

            if (!string.IsNullOrEmpty(entry.ImagePath))
            {
                var ext = GetExtensionFromUrl(entry.ImagePath);     // e.g. ".jpg"
                var filename = $"ab_image_{entry.Id:N}{ext}";            // e.g. "ab_image_<guid>.jpg"

                var storeResult = await ankiService.StoreMediaFileFromUrlAsync(filename, entry.ImagePath);

                if (storeResult.IsSuccess)
                {
                    // Anki field value rendered as an <img> by the card template
                    imageRef = storeResult.Value; // "ab_image_<guid>.jpg"
                }
            }

            result[entry.Id] = new MediaRefs(audioRef, imageRef);
        }

        return result;
    }

    private static AnkiNote ToCreateDto(Note note, MediaRefs? media)
    {
        var examples = note.LearningEntry.Examples
            .Select(x => x.Text)
            .ToArray();

        return new AnkiNote(
            NoteTypeName: note.NoteType.Name,
            DeckName: note.Deck.Name,
            Headword: note.LearningEntry.Headword,
            PartOfSpeech: note.LearningEntry.PartOfSpeech.ToString(),
            Ipa: note.LearningEntry.Ipa,
            Accent: note.LearningEntry.Accent.ToString(),
            Cloze: note.LearningEntry.Cloze,
            Definition: note.LearningEntry.Definition,
            Translation: note.LearningEntry.Translation,
            Example1: examples.ElementAtOrDefault(0) ?? string.Empty,
            Example2: examples.ElementAtOrDefault(1) ?? string.Empty,
            Example3: examples.ElementAtOrDefault(2) ?? string.Empty,
            AudioPath: media?.AudioRef ?? string.Empty,   // "[sound:ab_audio_<id>.mp3]"
            ImagePath: media?.ImageRef ?? string.Empty);  // "<img src='ab_image_<id>.jpg'>"
    }

    /// <summary>
    /// Extracts the file extension from an Azure Blob Storage URL.
    /// e.g. "http://127.0.0.1:10000/devstoreaccount1/uploads/word.mp3" → ".mp3"
    /// </summary>
    private static string GetExtensionFromUrl(string url)
    {
        try
        {
            return Path.GetExtension(new Uri(url).AbsolutePath);
        }
        catch
        {
            return string.Empty;
        }
    }

    // Carries the resolved Anki field values (already formatted for the card template).
    private sealed record MediaRefs(string? AudioRef, string? ImageRef);
}