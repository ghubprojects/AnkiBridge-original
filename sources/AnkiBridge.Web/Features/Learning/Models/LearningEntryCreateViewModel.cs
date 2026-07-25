using AnkiBridge.Application.Features.Dictionary.UseCases.GenerateAudio;
using AnkiBridge.Application.Features.Dictionary.UseCases.GetDictionaryEntry;
using AnkiBridge.Application.Features.Dictionary.UseCases.ResolveIpa;
using AnkiBridge.Application.Features.Dictionary.UseCases.SearchImages;
using AnkiBridge.Application.Features.Learning.UseCases.CreateLearningEntry;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Extensions;
using AnkiBridge.Shared.Results;
using AnkiBridge.Web.Common.Dispatching;
using AnkiBridge.Web.Common.Helpers;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace AnkiBridge.Web.Features.Learning.Models;

public sealed class LearningEntryCreateViewModel
{
    // ── BLAZOR FORM STATE ────────────────────────────────────────────────────
    public EditContext EditContext { get; private set; }
    public DictionaryEntryOption? SelectedEntry { get; set; }
    public bool IsSubmitting { get; set; }
    public bool IsScrapingOnline { get; set; }
    public bool IsGeneratingIpa { get; set; }
    public bool IsGeneratingAudio { get; set; }
    public bool IsLoadingMoreImages { get; set; }

    // ── CACHE DATA FOR DROPDOWNS ─────────────────────────────────────────────
    public List<string> DefinitionOptions { get; private set; } = [];
    public List<string> ExampleOptions { get; private set; } = [];
    public List<string> Images { get; private set; } = [];
    public int CurrentImageIndex { get; private set; }

    private int _imagePage = 1;
    private Dictionary<string, List<string>> _definitionExamplesMap = [];

    // ── FORM PROPERTIES & VALIDATIONS (Merged from DetailModel) ──────────────
    public Guid? DictionaryEntryId { get; set; }

    [Required(ErrorMessage = "Headword is required.")]
    [MaxLength(50, ErrorMessage = "Headword cannot exceed 50 characters.")]
    public string Headword { get; set; } = string.Empty;

    [Required]
    public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;

    [Required(ErrorMessage = "Cloze is required.")]
    public string Cloze { get; set; } = string.Empty;

    [Required(ErrorMessage = "Definition is required.")]
    public string Definition { get; set; } = string.Empty;

    public string Example1 { get; set; } = string.Empty;
    public string Example2 { get; set; } = string.Empty;
    public string Example3 { get; set; } = string.Empty;

    public List<string> Examples => new[] { Example1, Example2, Example3 }
        .Where(ex => !string.IsNullOrWhiteSpace(ex))
        .Select(ex => ex.Trim())
        .ToList();

    public TranslationSource TranslationSource { get; set; }

    [Required(ErrorMessage = "Translation is required.")]
    public string Translation { get; set; } = string.Empty;

    [Required]
    public Accent Accent { get; set; } = Accent.American;

    [Required(ErrorMessage = "IPA pronunciation is required.")]
    public string Ipa { get; set; } = string.Empty;

    // Audio properties
    public AudioSource? AudioSource { get; set; }
    public string? AudioAbsolutePath { get; set; }
    public string? AudioFileName { get; set; }
    public string? AudioContentType { get; set; }
    public string? AudioPreviewUrl { get; set; }
    public string? AudioUrl { get; set; }

    // Image properties
    public ImageSource? ImageSource { get; set; }
    public string? ImageAbsolutePath { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public string? ImagePreviewUrl { get; set; }
    public string? ImageUrl { get; set; }

    // ── ENUM OPTIONS BACKGROUND (Read-Only) ──────────────────────────────────
    public readonly IEnumerable<Option<PartOfSpeech>> PartOfSpeechOptions =
        Enum.GetValues<PartOfSpeech>().Select(x => new Option<PartOfSpeech> { Value = x, Text = x.ToString().Humanize() });

    public readonly IEnumerable<Option<TranslationSource>> TranslationSourceOptions =
        Enum.GetValues<TranslationSource>().Select(x => new Option<TranslationSource> { Value = x, Text = x.ToString().Humanize() });

    public readonly IEnumerable<Option<Accent>> AccentOptions =
        Enum.GetValues<Accent>().Select(x => new Option<Accent> { Value = x, Text = x.ToString().Humanize() });

    public readonly IEnumerable<Option<AudioSource>> AudioSourceOptions =
        Enum.GetValues<AudioSource>().Select(x => new Option<AudioSource> { Value = x, Text = x.ToString().Humanize() });

    public readonly IEnumerable<Option<ImageSource>> ImageSourceOptions =
        Enum.GetValues<ImageSource>().Select(x => new Option<ImageSource> { Value = x, Text = x.ToString().Humanize() });

    public LearningEntryCreateViewModel()
    {
        EditContext = new EditContext(this);
    }

    // ── FORM CORE ACTIONS ────────────────────────────────────────────────────
    public void ClearForm()
    {
        SelectedEntry = null;
        _definitionExamplesMap = [];
        _imagePage = 1;
        DefinitionOptions = [];
        ExampleOptions = [];
        Images = [];
        CurrentImageIndex = 0;

        // Reset data fields
        DictionaryEntryId = null;
        Headword = string.Empty;
        PartOfSpeech = PartOfSpeech.Noun;
        Cloze = string.Empty;
        Definition = string.Empty;
        Example1 = string.Empty;
        Example2 = string.Empty;
        Example3 = string.Empty;
        TranslationSource = TranslationSource.Google;
        Translation = string.Empty;
        Accent = Accent.American;
        Ipa = string.Empty;

        // Media Reset
        AudioSource = null; AudioAbsolutePath = null;
        AudioFileName = null; AudioContentType = null; AudioPreviewUrl = null; AudioUrl = null;

        ImageSource = null; ImageAbsolutePath = null;
        ImageFileName = null; ImageContentType = null; ImagePreviewUrl = null; ImageUrl = null;

        // Re-init context
        EditContext = new EditContext(this);
    }

    public void HandleDefinitionChanged(string? value)
    {
        Definition = value ?? string.Empty;

        if (!string.IsNullOrEmpty(value) && _definitionExamplesMap.TryGetValue(value, out var examples))
        {
            ExampleOptions = examples;
            Example1 = ExampleOptions.ElementAtOrDefault(0) ?? string.Empty;
            Example2 = ExampleOptions.ElementAtOrDefault(1) ?? string.Empty;
            Example3 = ExampleOptions.ElementAtOrDefault(2) ?? string.Empty;
        }
        else
        {
            ExampleOptions = [];
            Example1 = string.Empty;
            Example2 = string.Empty;
            Example3 = string.Empty;
        }
    }

    public void NextImage()
    {
        if (Images.Count <= 1) return;

        CurrentImageIndex = (CurrentImageIndex + 1) % Images.Count;
        DeleteTemporaryFile(ImageAbsolutePath);
        ImageAbsolutePath = null;
        ImagePreviewUrl = null;
        ImageUrl = Images[CurrentImageIndex];
    }

    public void ClearAudio()
    {
        DeleteTemporaryFile(AudioAbsolutePath);
        AudioUrl = null;
        AudioAbsolutePath = null;
        AudioFileName = null;
        AudioContentType = null;
        AudioPreviewUrl = null;
        AudioSource = null;
    }

    public void ClearImage()
    {
        DeleteTemporaryFile(ImageAbsolutePath);
        ImageUrl = null;
        ImageAbsolutePath = null;
        ImageFileName = null;
        ImageContentType = null;
        ImagePreviewUrl = null;
        ImageSource = null;
    }

    // ── MEDIA UPLOAD PROCESSORS ──────────────────────────────────────────────
    public async Task ProcessUploadedAudioAsync(string tempFilePath, string fileName, string contentType)
    {
        DeleteTemporaryFile(AudioAbsolutePath);
        AudioSource = Domain.Enums.AudioSource.User;
        AudioAbsolutePath = tempFilePath;
        AudioFileName = fileName;
        AudioContentType = contentType;

        var fileBytes = await File.ReadAllBytesAsync(tempFilePath);
        AudioPreviewUrl = $"data:{contentType};base64,{Convert.ToBase64String(fileBytes)}";
        AudioUrl = AudioPreviewUrl;
    }

    public async Task ProcessUploadedImageAsync(string tempFilePath, string fileName, string contentType)
    {
        DeleteTemporaryFile(ImageAbsolutePath);
        ImageSource = Domain.Enums.ImageSource.User;
        ImageAbsolutePath = tempFilePath;
        ImageFileName = fileName;
        ImageContentType = contentType;

        var fileBytes = await File.ReadAllBytesAsync(tempFilePath);
        ImagePreviewUrl = $"data:{contentType};base64,{Convert.ToBase64String(fileBytes)}";
        ImageUrl = ImagePreviewUrl;
    }

    // ── IPA GENERATION ───────────────────────────────────────────────────────

    public async Task<Result<string>> GenerateIpaAsync(IRequestDispatcher dispatcher)
    {
        var result = await dispatcher.Send(new TranscribeHeadwordQuery(Headword.Trim(), Accent));

        if (result.IsSuccess)
            Ipa = result.Value.WrapWithSlashes();

        return result;
    }

    // ── AUDIO GENERATION ─────────────────────────────────────────────────────

    /// <summary>
    /// Calls <see cref="GenerateAudioQuery"/> to build a Google TTS URL for the current headword.
    /// Sets <see cref="AudioUrl"/> and <see cref="AudioSource"/> on success.
    /// </summary>
    public async Task<Result<string>> GenerateAudioAsync(IRequestDispatcher dispatcher)
    {
        var result = await dispatcher.Send(new GenerateAudioQuery(Headword.Trim()));

        if (result.IsSuccess)
        {
            DeleteTemporaryFile(AudioAbsolutePath);
            AudioAbsolutePath = null;
            AudioFileName = null;
            AudioContentType = null;
            AudioUrl = result.Value;
            AudioSource = Domain.Enums.AudioSource.Google;
        }

        return result;
    }

    // ── IMAGE – LOAD MORE ────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the next page of 5 images and appends them to <see cref="Images"/>.
    /// Moves <see cref="CurrentImageIndex"/> to the first newly loaded image.
    /// </summary>
    public async Task<Result> LoadMoreImagesAsync(IRequestDispatcher dispatcher)
    {
        if (string.IsNullOrWhiteSpace(Headword))
            return Result.Failure("Enter a headword before loading images.");

        _imagePage++;

        var result = await dispatcher.Send(new GenerateImagesQuery(Headword.Trim(), Count: 5, Page: _imagePage));
        if (result.IsFailure)
        {
            _imagePage--; // rollback on failure
            return Result.Failure(result.Error.Message);
        }

        var newImages = result.Value;
        if (newImages.Count == 0)
        {
            _imagePage--;
            return Result.Failure("No more images found.");
        }

        var previousCount = Images.Count;
        Images.AddRange(newImages.Select(x => x.FullUrl));

        // Jump to first newly loaded image
        DeleteTemporaryFile(ImageAbsolutePath);
        ImageAbsolutePath = null;
        ImageFileName = null;
        ImageContentType = null;
        CurrentImageIndex = previousCount;
        ImageUrl = Images[CurrentImageIndex];
        ImageSource = newImages[0].Source;

        return Result.Success();
    }

    // ── DICTIONARY SERVICE MAPPING ───────────────────────────────────────────
    public async Task MapDictionaryEntryToFormAsync(Guid entryId, IRequestDispatcher dispatcher)
    {
        var result = await dispatcher.Send(new GetDictionaryEntryQuery(entryId));
        if (result.IsFailure) throw new InvalidOperationException(result.Error.Message);

        var entry = result.Value;
        ClearForm();

        DictionaryEntryId = entry.Id;
        Headword = entry.Headword;
        PartOfSpeech = entry.PartOfSpeech;
        Cloze = StringUtils.Mask(entry.Headword);

        var pronunciation = entry.Pronunciations.FirstOrDefault(x => x.Accent == Accent.American)
                            ?? entry.Pronunciations.FirstOrDefault();
        if (pronunciation is not null)
        {
            Accent = pronunciation.Accent;
            Ipa = pronunciation.Ipa.WrapWithSlashes();
            AudioUrl = pronunciation.AudioUrl;
            AudioSource = pronunciation.AudioSource;
        }

        var translationObj = entry.Translations.FirstOrDefault();
        if (translationObj is not null)
        {
            Translation = translationObj.Text;
            TranslationSource = TranslationSource.Google;
        }

        Images = entry.Images.Select(x => x.Url).ToList();
        CurrentImageIndex = 0;
        ImageUrl = Images.FirstOrDefault();
        if (!string.IsNullOrEmpty(ImageUrl))
            ImageSource = Domain.Enums.ImageSource.Pixabay;

        _definitionExamplesMap = entry.Definitions.ToDictionary(x => x.Text, x => x.Examples.Select(e => e.Text).ToList());
        DefinitionOptions = [.. _definitionExamplesMap.Keys];

        var firstDefinition = entry.Definitions.FirstOrDefault();
        if (firstDefinition is not null)
        {
            Definition = firstDefinition.Text;
            ExampleOptions = firstDefinition.Examples.Select(e => e.Text).ToList();

            Example1 = ExampleOptions.ElementAtOrDefault(0) ?? string.Empty;
            Example2 = ExampleOptions.ElementAtOrDefault(1) ?? string.Empty;
            Example3 = ExampleOptions.ElementAtOrDefault(2) ?? string.Empty;
        }
    }

    public async Task<Result<Guid>> SaveAsync(IRequestDispatcher dispatcher)
    {
        var command = new CreateLearningEntryCommand(
            DictionaryEntryId,
            Headword,
            PartOfSpeech,
            Cloze,
            Definition,
            Examples,
            TranslationSource,
            Translation,
            Accent,
            Ipa,

            // Audio
            AudioSource,
            AudioSource == Domain.Enums.AudioSource.User ? null : AudioUrl,
            AudioAbsolutePath,
            AudioFileName,
            AudioContentType,

            // Image
            ImageSource,
            ImageSource == Domain.Enums.ImageSource.User ? null : ImageUrl,
            ImageAbsolutePath,
            ImageFileName,
            ImageContentType
        );

        return await dispatcher.Send(command);
    }

    private static void DeleteTemporaryFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch
        {
            // The OS temp directory is cleaned periodically; failure here must not block the form.
        }
    }
}
