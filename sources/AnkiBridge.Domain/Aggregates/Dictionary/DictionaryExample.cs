using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public sealed class DictionaryExample : Entity<Guid>
{
    public string Text { get; private set; } = default!;

    #region Constructors

    private DictionaryExample() { }

    private DictionaryExample(string text)
    {
        Id = Guid.CreateVersion7();
        Text = text;
    }

    #endregion

    #region Factory Method

    internal static Result<DictionaryExample> Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<DictionaryExample>("Example text must not be empty.");

        return new DictionaryExample(text.Trim());
    }

    #endregion
}