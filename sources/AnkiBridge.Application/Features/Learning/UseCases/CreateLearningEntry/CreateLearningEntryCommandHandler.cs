using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Learning.UseCases.CreateLearningEntry;

public sealed class CreateLearningEntryCommandHandler(
    ILearningEntryRepository learningEntryRepository)
    : IRequestHandler<CreateLearningEntryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLearningEntryCommand request, CancellationToken cancellationToken)
    {
        // 1. Khởi tạo LearningEntry ban đầu thông qua Factory method
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
            request.Ipa
        );

        if (createResult.IsFailure)
            return createResult.ToFailure<Guid>();

        var learningEntry = createResult.Value;

        // Trạng thái biến trung gian dùng để đẩy vào Event dữ liệu chính xác
        string? targetAudioPath = null;
        string? targetImagePath = null;

        /**
        // 2. Phân định nguồn Audio qua Domain API
        if (request.AudioSource == AudioSource.Dictionary)
        {
            learningEntry.SetAudioFromDictionary(request.AudioRelativePath);
            targetAudioPath = learningEntry.AudioPath;
        }
        else if (request.AudioSource == AudioSource.User && !string.IsNullOrWhiteSpace(request.AudioAbsolutePath))
        {
            // Yêu cầu domain sinh cấu trúc lưu trữ chính thức
            targetAudioPath = learningEntry.PrepareUserAudioProcessing(request.AudioFileName ?? "audio.mp3");
        }

        // 3. Phân định nguồn Image qua Domain API
        if (request.ImageSource == ImageSource.Dictionary)
        {
            learningEntry.SetImageFromDictionary(request.ImageRelativePath);
            targetImagePath = learningEntry.ImagePath;
        }
        else if (request.ImageSource == ImageSource.User && !string.IsNullOrWhiteSpace(request.ImageAbsolutePath))
        {
            // Yêu cầu domain sinh cấu trúc lưu trữ chính thức
            targetImagePath = learningEntry.PrepareUserImageProcessing(request.ImageFileName ?? "image.jpg");
        }

        // 4. Gắn Event chứa toàn bộ dữ liệu chỉ thị cho Outbox Background Worker xử lý I/O vật lý sau
        learningEntry.AddDomainEvent(new LearningEntryCreatedEvent(
            learningEntry.Id,
            
            request.AudioSource,
            request.AudioAbsolutePath, // Thư mục tạm bên phía UI tải lên, Worker cần đọc từ đây
            targetAudioPath,           // Vị trí lưu trữ chính thức trong Storage
            request.AudioFileName,
            request.AudioContentType,

            request.ImageSource,
            request.ImageAbsolutePath, // Thư mục tạm bên phía UI tải lên, Worker cần đọc từ đây
            targetImagePath,           // Vị trí lưu trữ chính thức trong Storage
            request.ImageFileName,
            request.ImageContentType
        ));
        */

        // 5. Đẩy xuống Repository. Tiến hành SaveChanges để đồng bộ Entity + Outbox Message trong cùng một DB Transaction.
        await learningEntryRepository.AddAsync(learningEntry, cancellationToken);

        await learningEntryRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(learningEntry.Id);
    }
}