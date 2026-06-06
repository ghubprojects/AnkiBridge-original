using System;
using System.Collections.Generic;
using System.Text;

namespace AnkiBridge.Infrastructure.Services.Speech;

/// <summary>
/// Configuration for <see cref="GoogleSpeechSynthesizer"/>.
/// Bind from appsettings section <c>Speech:Google</c>.
/// </summary>
public sealed class GoogleSpeechOptions
{
    public const string SectionName = "Speech:Google";

    /// <summary>
    /// Default BCP-47 language tag used when the caller does not specify one.
    /// Defaults to <c>en</c>.
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// The <c>client</c> query-string parameter sent to the Google TTS endpoint.
    /// Using <c>tw-ob</c> works without an API key for lightweight usage.
    /// Switch to an authenticated client value if you have a Cloud TTS key.
    /// </summary>
    public string Client { get; set; } = "tw-ob";
}
