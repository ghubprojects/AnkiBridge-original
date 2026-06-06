using AnkiBridge.Application.Features.Flashcard.Contracts.Anki.Models;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Features.Flashcard.Contracts.Anki;

public interface IAnkiService
{
    public Task<Result<List<AnkiDeck>>> GetDecksAsync(
        CancellationToken cancellationToken = default);

    public Task<Result<List<AnkiNoteType>>> GetNoteTypesAsync(
        CancellationToken cancellationToken = default);

    public Task<Result<List<long>>> AddNotesAsync(
        IReadOnlyList<AnkiNote> notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a media file in Anki's media folder by downloading it from a URL.
    /// Returns the actual filename Anki stored (may differ if conflict resolution occurs).
    /// </summary>
    Task<Result<string>> StoreMediaFileFromUrlAsync(
        string filename,
        string url,
        CancellationToken cancellationToken = default);
}
