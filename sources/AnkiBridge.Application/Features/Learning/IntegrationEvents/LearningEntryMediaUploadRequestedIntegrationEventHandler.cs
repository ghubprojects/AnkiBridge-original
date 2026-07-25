using AnkiBridge.Application.Common.Contracts.Storage;
using AnkiBridge.Application.Common.IntegrationEvents;
using AnkiBridge.Domain.Aggregates.Learning;
using Microsoft.Extensions.Logging;

namespace AnkiBridge.Application.Features.Learning.IntegrationEvents;

public sealed class LearningEntryMediaUploadRequestedIntegrationEventHandler(
    ILearningEntryRepository learningEntryRepository,
    IFileStorage fileStorage,
    IRemoteMediaSource remoteMediaSource,
    ILogger<LearningEntryMediaUploadRequestedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<LearningEntryMediaUploadRequestedIntegrationEvent>
{
    public async Task Handle(LearningEntryMediaUploadRequestedIntegrationEvent integrationEvent)
    {
        var learningEntry = await learningEntryRepository.GetByIdAsync(integrationEvent.LearningEntryId);
        if (learningEntry is null)
        {
            logger.LogWarning(
                "Skipping media upload because learning entry {LearningEntryId} no longer exists.",
                integrationEvent.LearningEntryId);
            return;
        }

        BeginUpload(learningEntry, integrationEvent.MediaKind);

        try
        {
            var blobUri = await UploadAsync(integrationEvent);
            CompleteUpload(learningEntry, integrationEvent.MediaKind, blobUri);
            await learningEntryRepository.UnitOfWork.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            MarkUploadFailed(learningEntry, integrationEvent.MediaKind, exception.Message);
            await learningEntryRepository.UnitOfWork.SaveChangesAsync();
            throw;
        }

        DeleteLocalFileAfterUpload(integrationEvent.LocalFilePath);
    }

    private async Task<string> UploadAsync(
        LearningEntryMediaUploadRequestedIntegrationEvent integrationEvent)
    {
        if (!string.IsNullOrWhiteSpace(integrationEvent.LocalFilePath))
        {
            await using var localStream = new FileStream(
                integrationEvent.LocalFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return await UploadStreamAsync(localStream, integrationEvent, integrationEvent.ContentType);
        }

        if (string.IsNullOrWhiteSpace(integrationEvent.SourceUrl))
            throw new InvalidOperationException("The media upload request has no source.");

        await using var remoteMedia = await remoteMediaSource.OpenReadAsync(integrationEvent.SourceUrl);
        var contentType = remoteMedia.ContentType ?? integrationEvent.ContentType;

        return await UploadStreamAsync(remoteMedia.Stream, integrationEvent, contentType);
    }

    private async Task<string> UploadStreamAsync(
        Stream stream,
        LearningEntryMediaUploadRequestedIntegrationEvent integrationEvent,
        string contentType)
    {
        var uploadResult = await fileStorage.UploadAsync(
            stream,
            integrationEvent.BlobName,
            contentType);

        if (uploadResult.IsFailure)
            throw new InvalidOperationException(uploadResult.Error.Message);

        return uploadResult.Value;
    }

    private void DeleteLocalFileAfterUpload(string? localFilePath)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
            return;

        try
        {
            File.Delete(localFilePath);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Uploaded media but could not delete local file {LocalFilePath}.",
                localFilePath);
        }
    }

    private static void BeginUpload(LearningEntry learningEntry, LearningEntryMediaKind mediaKind)
    {
        if (mediaKind == LearningEntryMediaKind.Audio)
            learningEntry.BeginAudioUpload();
        else
            learningEntry.BeginImageUpload();
    }

    private static void CompleteUpload(
        LearningEntry learningEntry,
        LearningEntryMediaKind mediaKind,
        string blobUri)
    {
        if (mediaKind == LearningEntryMediaKind.Audio)
            learningEntry.CompleteAudioUpload(blobUri);
        else
            learningEntry.CompleteImageUpload(blobUri);
    }

    private static void MarkUploadFailed(
        LearningEntry learningEntry,
        LearningEntryMediaKind mediaKind,
        string error)
    {
        var safeError = error.Length <= 500 ? error : error[..500];

        if (mediaKind == LearningEntryMediaKind.Audio)
            learningEntry.MarkAudioUploadFailed(safeError);
        else
            learningEntry.MarkImageUploadFailed(safeError);
    }
}
