using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Abstractions.Images;

public sealed record ImageResult(
    string PreviewUrl,
    string FullUrl,
    ImageSource Source);
