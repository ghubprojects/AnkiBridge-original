using AnkiBridge.Domain.Enums;
using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public sealed class DictionaryImage : Entity<Guid>
{
    public string Url { get; private set; } = default!;
    public ImageSource Source { get; private set; }

    #region Constructors

    private DictionaryImage() { }

    private DictionaryImage(string url, ImageSource source)
    {
        Id = Guid.CreateVersion7();
        Url = url;
        Source = source;
    }

    #endregion

    #region Factory Method

    internal static Result<DictionaryImage> Create(string url, ImageSource source)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure<DictionaryImage>("Image URL must not be empty.");

        return new DictionaryImage(url.Trim(), source);
    }

    #endregion
}
