using System.Text.Json;
using AnkiBridge.Application.Common.Contracts.Outbox;
using AnkiBridge.Application.Common.IntegrationEvents;
using AnkiBridge.Application.Features.Learning.IntegrationEvents;
using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Learning.UseCases.CreateLearningEntry;

public sealed class CreateLearningEntryCommandHandler(
    ILearningEntryRepository learningEntryRepository,
    IOutboxMessageRepository outboxMessageRepository)
    : IRequestHandler<CreateLearningEntryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateLearningEntryCommand request,
        CancellationToken cancellationToken)
    {
        var createResult = LearningEntry.Create(
            request.DictionaryEntryId,
            request.Headword,
            request.PartOfSpeech,
            request.Cloze,
            request.Definition,
            request.Examples,
            request.TranslationSource,
            request.Translation,
            request.Accent,
            request.Ipa);

        if (createResult.IsFailure)
            return createResult.ToFailure<Guid>();

        var learningEntry = createResult.Value;
        var mediaEvents = new List<LearningEntryMediaUploadRequestedIntegrationEvent>();

        if (request.AudioSource is { } audioSource)
        {
            learningEntry.QueueAudioUpload(audioSource);
            mediaEvents.Add(new LearningEntryMediaUploadRequestedIntegrationEvent(
                learningEntry.Id,
                LearningEntryMediaKind.Audio,
                BuildBlobName(
                    learningEntry.Id,
                    "audio",
                    request.AudioFileName,
                    request.AudioSourceUrl,
                    ".mp3"),
                request.AudioContentType ?? "audio/mpeg",
                audioSource == AudioSource.User ? null : request.AudioSourceUrl,
                audioSource == AudioSource.User ? request.AudioAbsolutePath : null));
        }

        if (request.ImageSource is { } imageSource)
        {
            learningEntry.QueueImageUpload(imageSource);
            mediaEvents.Add(new LearningEntryMediaUploadRequestedIntegrationEvent(
                learningEntry.Id,
                LearningEntryMediaKind.Image,
                BuildBlobName(
                    learningEntry.Id,
                    "image",
                    request.ImageFileName,
                    request.ImageSourceUrl,
                    ".jpg"),
                request.ImageContentType ?? "image/jpeg",
                imageSource == ImageSource.User ? null : request.ImageSourceUrl,
                imageSource == ImageSource.User ? request.ImageAbsolutePath : null));
        }

        await learningEntryRepository.AddAsync(learningEntry, cancellationToken);

        foreach (var mediaEvent in mediaEvents)
        {
            var payload = JsonSerializer.Serialize(
                mediaEvent,
                mediaEvent.GetType(),
                IntegrationEventSubscriptionInfo.DefaultSerializerOptions);

            await outboxMessageRepository.AddAsync(
                new OutboxMessage(payload, mediaEvent.GetType().FullName!),
                cancellationToken);
        }

        await learningEntryRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(learningEntry.Id);
    }

    private static string BuildBlobName(
        Guid learningEntryId,
        string mediaFolder,
        string? originalFileName,
        string? sourceUrl,
        string fallbackExtension)
    {
        var extension = GetSafeExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension)
            && Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri))
        {
            extension = GetSafeExtension(sourceUri.AbsolutePath);
        }

        extension = string.IsNullOrWhiteSpace(extension) ? fallbackExtension : extension;

        return $"learning-entries/{learningEntryId:N}/{mediaFolder}/{Guid.NewGuid():N}{extension}";
    }

    private static string GetSafeExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var extension = Path.GetExtension(Path.GetFileName(fileName));
        if (extension.Length is < 2 or > 10
            || extension.Skip(1).Any(character => !char.IsLetterOrDigit(character)))
        {
            return string.Empty;
        }

        return extension.ToLowerInvariant();
    }
}
