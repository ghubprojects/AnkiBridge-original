using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Learning;

public sealed class LearningExample : Entity<Guid>
{
    public string Text { get; private set; } = default!;

    private LearningExample() { }

    private LearningExample(string text)
    {
        Id = Guid.CreateVersion7();
        Text = text;
    }

    internal static Result<LearningExample> Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<LearningExample>("Example text cannot be empty.");

        return new LearningExample(text.Trim());
    }

    internal Result UpdateText(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            return Result.Failure("Example text cannot be empty.");

        if (Text != newText)
            Text = newText.Trim();

        return Result.Success();
    }
}