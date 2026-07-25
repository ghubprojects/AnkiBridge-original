using AnkiBridge.Application.Features.Learning.Contracts.QueryServices.Models;
using AnkiBridge.Domain.Aggregates.Learning;
using AnkiBridge.Shared.Results;
using MediatR;

namespace AnkiBridge.Application.Features.Learning.UseCases.GetLearningEntry;

public sealed class GetLearningEntryQueryHandler(
    ILearningEntryRepository LearningEntryRepository)
    : IRequestHandler<GetLearningEntryQuery, Result<LearningEntryDetail>>
{
    public async Task<Result<LearningEntryDetail>> Handle(
        GetLearningEntryQuery request,
        CancellationToken cancellationToken)
    {
        var item = await LearningEntryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (item is null)
            return Result.Failure<LearningEntryDetail>("Learning item not found.", ErrorType.NotFound);

        return new LearningEntryDetail(
            item.Id,
            item.Headword,
            item.PartOfSpeech,
            item.Accent,
            item.Ipa,
            item.Cloze,
            item.Definition,
            item.Translation,
            item.AudioPath,
            item.AudioUploadStatus,
            item.AudioUploadError,
            item.ImagePath,
            item.ImageUploadStatus,
            item.ImageUploadError,
            item.Examples
                .Select(x => new LearningEntryDetailExample(x.Id, x.Text))
                .ToList(),
            item.CreatedAt);
    }
}
