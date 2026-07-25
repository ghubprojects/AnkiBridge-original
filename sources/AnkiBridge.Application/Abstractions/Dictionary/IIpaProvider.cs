using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Application.Abstractions.Dictionary;

public interface IIpaProvider
{
    Task<Result<string>> TranscribeAsync(
        string headword,
        Accent accent,
        CancellationToken cancellationToken = default);
}
