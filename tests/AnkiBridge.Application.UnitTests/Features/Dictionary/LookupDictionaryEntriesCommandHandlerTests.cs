using AnkiBridge.Application.Abstractions.Dictionary;
using AnkiBridge.Application.Abstractions.Images;
using AnkiBridge.Application.Abstractions.Speech;
using AnkiBridge.Application.Abstractions.Translation;
using AnkiBridge.Application.Features.Dictionary.UseCases.LookupDictionaryEntries;
using AnkiBridge.Domain.Aggregates.Dictionary;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;
using FluentAssertions;
using Moq;

namespace AnkiBridge.Application.UnitTests.Features.Dictionary;

public sealed class LookupDictionaryEntriesCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntryAlreadyExists_ReturnsConflictWithoutSaving()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var foundEntry = new DictionaryEntryResult(
            "test",
            PartOfSpeech.Noun,
            DictionarySource.Cambridge,
            [],
            []);

        var dictionaryEntryProvider = new Mock<IDictionaryEntryProvider>();
        dictionaryEntryProvider
            .Setup(x => x.LookupAsync("test", cancellationToken))
            .ReturnsAsync(Result.Success<IReadOnlyList<DictionaryEntryResult>>([foundEntry]));

        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IDictionaryEntryRepository>();
        repository.SetupGet(x => x.UnitOfWork).Returns(unitOfWork.Object);
        repository
            .Setup(x => x.ExistsAsync(
                "test",
                PartOfSpeech.Noun,
                DictionarySource.Cambridge,
                cancellationToken))
            .ReturnsAsync(true);

        var translationProvider = new Mock<ITranslationProvider>();
        var imageProvider = new Mock<IImageProvider>();
        var handler = new LookupDictionaryEntriesCommandHandler(
            dictionaryEntryProvider.Object,
            translationProvider.Object,
            new Mock<IIpaProvider>().Object,
            new Mock<IAudioProvider>().Object,
            imageProvider.Object,
            repository.Object);

        var result = await handler.Handle(
            new LookupDictionaryEntriesCommand(" test "),
            cancellationToken);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Be(
            "Dictionary entry 'test' (Noun, Cambridge) already exists.");
        repository.Verify(x => x.Add(It.IsAny<DictionaryEntry>()), Times.Never);
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        translationProvider.Verify(
            x => x.TranslateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        imageProvider.Verify(
            x => x.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
