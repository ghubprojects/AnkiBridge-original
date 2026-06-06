using AnkiBridge.Application.Features.Dictionary.UseCases.GetDictionaryEntry;
using AnkiBridge.Application.Features.Dictionary.UseCases.ScrapeDictionaryEntry;
using AnkiBridge.Application.Features.Dictionary.UseCases.SearchDictionaryEntries;
using AnkiBridge.Application.Features.Learning.UseCases.CreateLearningEntry;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Extensions;
using AnkiBridge.Web.Common.Helpers;
using AnkiBridge.Web.Features.Learning.Helpers;
using AnkiBridge.Web.Features.Learning.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AnkiBridge.Web.Features.Learning.Pages;

public partial class LearningEntryCreate
{
    // ── Inject ──────────────────────────────────────────────────────────────────

    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private EditContext editContext = default!;
    private LearningEntryDetailModel Detail { get; set; } = new();
    private DictionaryEntryOption? _selectedEntry;

    private bool IsSubmitting { get; set; }

    private IBrowserFile? audioFile;
    private IBrowserFile? imageFile;

    private readonly IEnumerable<Option<PartOfSpeech>> partOfSpeechOptions =
        SelectOptions.OrderedPartsOfSpeech.Select(x => new Option<PartOfSpeech>
        {
            Value = x,
            Text = x.ToString().Humanize()
        });

    private readonly IEnumerable<Option<Accent>> accentOptions =
        SelectOptions.OrderedAccents.Select(x => new Option<Accent>
        {
            Value = x,
            Text = SelectOptions.AccentDisplayLabels.TryGetValue(x, out var label)
                ? label
                : x.ToString().Humanize()
        });

    private List<string> definitionOptions = [];
    private List<string> exampleOptions = [];

    // sau dòng private List<string> exampleOptions = [];
    private Dictionary<string, List<string>> _definitionExamplesMap = new();
    private List<string> _images = [];
    private int _currentImageIndex = 0;

    protected override void OnInitialized()
    {
        editContext = new EditContext(Detail);
    }

    // ── Form helpers ─────────────────────────────────────────────────────────

    private void HandleSelectPartOfSpeech(string? value)
    {
        if (Enum.TryParse<PartOfSpeech>(value, out var result))
            Detail.PartOfSpeech = result;
    }

    private void HandleSelectAccent(string? value)
    {
        if (Enum.TryParse<Accent>(value, out var result))
            Detail.Accent = result;
    }

    private void AddExample()
    {
        if (Detail.Examples.Count < 3)
            Detail.Examples.Add(string.Empty);
    }

    private void RemoveExample(int index)
    {
        if (index >= 0 && index < Detail.Examples.Count)
            Detail.Examples.RemoveAt(index);
    }

    private void ClearForm()
    {
        audioFile = null;
        imageFile = null;
        _selectedEntry = null;
        _definitionExamplesMap = new();
        _images = [];
        _currentImageIndex = 0;
        Detail = new LearningEntryDetailModel();
        editContext = new EditContext(Detail);
    }

    // ── Anki / Dictionary handlers ──────────────────────────────────────────

    private async Task ScrapeFromDictionaryOnlineAsync()
    {
        await Dispatcher
            .Send(new ScrapeDictionaryEntryCommand(Detail.Headword))
            .Match(
                async () => ToastService.ShowSuccess("Dictionary entry scraped successfully."),
                error => DialogService.ShowErrorAsync(error.Message));
    }

    private async Task SearchDictionaryEntriesAsync(OptionsSearchEventArgs<DictionaryEntryOption> e)
    {
        await Dispatcher
            .Send(new SearchDictionaryEntriesQuery(e.Text))
            .Match(
                async data =>
                {
                    e.Items = data.Select(x => new DictionaryEntryOption(
                        x.Id,
                        Headword: x.Headword,
                        DisplayName: $"{x.Headword} ({x.PartOfSpeech.ToString().ToLowerInvariant()})"));
                },
                error => DialogService.ShowErrorAsync(error.Message));
    }

    /// <summary>
    /// Wrapper cho SelectedOptionChanged — đúng pattern Multiple=false theo FluentUI docs.
    /// Sau khi gọi SelectDictionaryEntry (có ClearForm bên trong), _selectedEntry = null
    /// và input hiển thị Detail.Headword qua ValueText binding.
    /// </summary>
    private async Task OnSelectedEntryChangedAsync(DictionaryEntryOption? option)
    {
        _selectedEntry = option;

        if (option is not null)
            await SelectDictionaryEntry(option);
    }

    private async Task SelectDictionaryEntry(DictionaryEntryOption option)
    {
        await Dispatcher
            .Send(new GetDictionaryEntryQuery(option.Id))
            .Match(
                async entryDetail =>
                {
                    // ClearForm reset _selectedEntry = null; input sẽ dùng ValueText = Detail.Headword
                    ClearForm();

                    Detail.Headword = entryDetail.Headword;
                    Detail.PartOfSpeech = entryDetail.PartOfSpeech;

                    var pronunciation = entryDetail.Pronunciations
                        .FirstOrDefault(x => x.Accent == Accent.American)
                        ?? entryDetail.Pronunciations.FirstOrDefault();

                    if (pronunciation is not null)
                    {
                        Detail.Accent = pronunciation.Accent;
                        Detail.Ipa = pronunciation.Ipa.WrapWithSlashes();
                        Detail.AudioUrl = pronunciation.AudioUrl;
                    }

                    Detail.Cloze = StringUtils.Mask(Detail.Headword);

                    _definitionExamplesMap = entryDetail.Definitions
                        .ToDictionary(x => x.Text, x => x.Examples.ToList());

                    definitionOptions = _definitionExamplesMap.Keys.ToList();

                    _images = entryDetail.Images.ToList();
                    _currentImageIndex = 0;

                    var definition = entryDetail.Definitions.FirstOrDefault();
                    if (definition is not null)
                    {
                        Detail.Definition = definition.Text;

                        exampleOptions = definition.Examples.ToList();
                        Detail.Examples = definition.Examples.Take(3).ToList();

                        if (Detail.Examples.Count == 0)
                            Detail.Examples.Add(string.Empty);
                    }

                    Detail.ImageUrl = _images.FirstOrDefault();
                },
                error => DialogService.ShowErrorAsync(error.Message));
    }

    // ── Definition / Image helpers ───────────────────────────────────────────────

    private void HandleDefinitionChanged(string? value)
    {
        Detail.Definition = value ?? string.Empty;

        if (!string.IsNullOrEmpty(value)
            && _definitionExamplesMap.TryGetValue(value, out var examples))
        {
            exampleOptions = examples;
            Detail.Examples = examples.Take(3).ToList();

            if (Detail.Examples.Count == 0)
                Detail.Examples.Add(string.Empty);
        }
    }

    private void NextImage()
    {
        if (_images.Count <= 1) return;
        _currentImageIndex = (_currentImageIndex + 1) % _images.Count;
        imageFile = null;                          // bỏ file upload thủ công nếu có
        Detail.ImageUrl = _images[_currentImageIndex];
    }

    // ── Media handlers ──────────────────────────────────────────────────────

    private async Task HandleChangeAudioAsync(InputFileChangeEventArgs e)
    {
        audioFile = e.File;
        Detail.AudioUrl = await ReadFileAsDataUrlAsync(audioFile);
    }

    private async Task HandleChangeImageAsync(InputFileChangeEventArgs e)
    {
        imageFile = e.File;
        Detail.ImageUrl = await ReadFileAsDataUrlAsync(imageFile);
    }

    private void ClearAudio()
    {
        audioFile = null;
        Detail.AudioUrl = null;
    }

    private void ClearImage()
    {
        imageFile = null;
        Detail.ImageUrl = null;
    }

    private static async Task<string> ReadFileAsDataUrlAsync(IBrowserFile file)
    {
        using var stream = file.OpenReadStream(10 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    // ── Submit ──────────────────────────────────────────────────────────────────

    private async Task SubmitAsync()
    {
        if (!editContext.Validate())
            return;

        IsSubmitting = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            // Resolve audio và image: ưu tiên file upload, fallback về online URL
            var (audioStream, audioFileName, audioContentType) =
                await ResolveMediaAsync(audioFile, Detail.AudioUrl, "audio/mpeg");

            var (imageStream, imageFileName, imageContentType) =
                await ResolveMediaAsync(imageFile, Detail.ImageUrl, "image/jpeg");

            await using (audioStream)
            await using (imageStream)
            {
                var command = new CreateLearningEntryCommand(
                    Headword: Detail.Headword,
                    PartOfSpeech: Detail.PartOfSpeech,
                    Ipa: Detail.Ipa,
                    Accent: Detail.Accent,
                    Cloze: Detail.Cloze,
                    Definition: Detail.Definition,
                    Translation: Detail.Translation,
                    Examples: Detail.Examples,
                    AudioStream: audioStream,
                    AudioFileName: audioFileName,
                    AudioContentType: audioContentType,
                    ImageStream: imageStream,
                    ImageFileName: imageFileName,
                    ImageContentType: imageContentType,
                    DictionaryEntryId: null);

                var result = await Dispatcher.Send(command);

                await result.Match(
                    async _ =>
                    {
                        ToastService.ShowSuccess("Learning item created successfully.");
                        ClearForm();
                    },
                    error => DialogService.ShowErrorAsync(error.Message));
            }
        }
        finally
        {
            IsSubmitting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Resolve stream cho media:
    /// - Nếu user upload thủ công → dùng IBrowserFile
    /// - Nếu chọn từ dictionary (online URL) → download về dạng MemoryStream
    /// - Không có gì → trả về (null, null, null)
    /// </summary>
    private async Task<(Stream? stream, string? fileName, string? contentType)> ResolveMediaAsync(
        IBrowserFile? file,
        string? url,
        string fallbackContentType)
    {
        if (file is not null)
            return (file.OpenReadStream(10 * 1024 * 1024), file.Name, file.ContentType);

        if (!string.IsNullOrEmpty(url) && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var http = HttpClientFactory.CreateClient();
            var bytes = await http.GetByteArrayAsync(url);
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            return (new MemoryStream(bytes), fileName, fallbackContentType);
        }

        return (null, null, null);
    }
}
