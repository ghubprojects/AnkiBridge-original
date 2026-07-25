using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Learning;

public sealed class LearningExample : Entity<Guid>
{
    public string Text { get; private set; } = default!;

    #region Constructor

    private LearningExample() { }

    private LearningExample(string text)
    {
        Id = Guid.CreateVersion7();
        Text = text;
    }

    #endregion

    #region Behavior Methods

    internal static Result<LearningExample> Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<LearningExample>("Example text must not be empty.");

        return new LearningExample(text.Trim());
    }

    internal Result Update(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure("Example text must not be empty.");

        var trimmed = text.Trim();
        if (Text != trimmed)
            Text = trimmed;

        return Result.Success();
    }

    #endregion
}