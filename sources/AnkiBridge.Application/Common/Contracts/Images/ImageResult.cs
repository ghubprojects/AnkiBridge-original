using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Common.Contracts.Images;

public sealed record ImageResult(
    string PreviewUrl,
    string FullUrl,
    ImageSource Source);