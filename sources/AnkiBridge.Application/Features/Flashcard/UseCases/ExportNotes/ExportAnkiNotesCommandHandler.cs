using AnkiBridge.Application.Features.Flashcard.IntegrationEvents;
using AnkiBridge.Shared.Results;
using MediatR;
using System.Text.Json;
using AnkiBridge.Application.Common.Contracts.Outbox;
using AnkiBridge.Domain.Aggregates.Flashcard.Notes;

namespace AnkiBridge.Application.Features.Flashcard.UseCases.ExportNotes;

public sealed class ExportAnkiNotesCommandHandler(
    INoteRepository noteRepository,
    IOutboxMessageRepository outboxMessageRepository)
    : IRequestHandler<ExportNotesCommand, Result>
{
    public async Task<Result> Handle(ExportNotesCommand request, CancellationToken cancellationToken)
    {
        var ids = request.NoteIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return Result.ValidationFailure(
                [new ValidationError(nameof(ExportNotesCommand.NoteIds), "Ids cannot be empty.")]);
        }

        if (ids.Length != request.NoteIds.Count)
        {
            return Result.ValidationFailure(
                [new ValidationError(nameof(ExportNotesCommand.NoteIds), "Ids must be unique.")]);
        }

        var notes = await noteRepository.GetByIdsAsync(
            ids,
            includeRelated: false,
            cancellationToken);

        var existingIds = notes
            .Select(x => x.Id)
            .ToHashSet();

        var missingIds = ids
            .Where(id => !existingIds.Contains(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            return Result.Failure(
                $"Anki notes not found: {string.Join(", ", missingIds)}.",
                ErrorType.NotFound);
        }

        foreach (var note in notes)
        {
            var markResult = note.MarkAsProcessingSilently();
            if (markResult.IsFailure)
                return markResult;
        }

        var integrationEvent = new NotesExportStartedIntegrationEvent(ids);

        var message = new OutboxMessage(
            JsonSerializer.Serialize(integrationEvent),
            integrationEvent.GetType().FullName!);

        await outboxMessageRepository.AddAsync(message, cancellationToken);

        await noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}