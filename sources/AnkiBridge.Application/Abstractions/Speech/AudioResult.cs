using AnkiBridge.Domain.Enums;

namespace AnkiBridge.Application.Abstractions.Speech;

public sealed record AudioResult(string Url, AudioSource Source);
