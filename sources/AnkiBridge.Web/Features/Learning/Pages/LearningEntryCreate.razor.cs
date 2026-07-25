using AnkiBridge.Application.Features.Dictionary.UseCases.ScrapeDictionaryEntry;
using AnkiBridge.Application.Features.Dictionary.UseCases.SearchDictionaryEntries;
using AnkiBridge.Web.Features.Learning.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AnkiBridge.Web.Features.Learning.Pages;

public partial class LearningEntryCreate
{
    private LearningEntryCreateViewModel ViewModel { get; } = new();

    private async Task ScrapeFromDictionaryOnlineAsync()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Headword))
        {
            ToastService.ShowWarning("Please enter a headword to scrape.");
            return;
        }

        ViewModel.IsScrapingOnline = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var currentHeadword = ViewModel.Headword.Trim();

            var scrapeResult = await Dispatcher.Send(new ScrapeDictionaryEntriesCommand(currentHeadword));
            if (scrapeResult.IsFailure)
            {
                await DialogService.ShowErrorAsync(scrapeResult.Error.Message);
                return;
            }

            ToastService.ShowSuccess("Dictionary entry scraped successfully.");

            var searchResult = await Dispatcher.Send(new SearchDictionaryEntriesQuery(currentHeadword));
            if (searchResult.IsFailure)
            {
                await DialogService.ShowErrorAsync(searchResult.Error.Message);
                return;
            }

            var match = searchResult.Value
                            .FirstOrDefault(x => x.Headword.Equals(currentHeadword, StringComparison.OrdinalIgnoreCase))
                        ?? searchResult.Value.FirstOrDefault();

            if (match is null) return;

            var option = new DictionaryEntryOption(
                match.Id,
                Headword: match.Headword,
                DisplayName: $"{match.Headword} ({match.PartOfSpeech.ToString().ToLowerInvariant()})");

            await OnSelectedEntryChangedAsync(option);
        }
        finally
        {
            ViewModel.IsScrapingOnline = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SearchDictionaryEntriesAsync(OptionsSearchEventArgs<DictionaryEntryOption> e)
    {
        var result = await Dispatcher.Send(new SearchDictionaryEntriesQuery(e.Text));
        if (result.IsFailure)
        {
            await DialogService.ShowErrorAsync(result.Error.Message);
            return;
        }

        e.Items = result.Value.Select(x => new DictionaryEntryOption(
            x.Id,
            Headword: x.Headword,
            DisplayName: $"{x.Headword} ({x.PartOfSpeech.ToString().ToLowerInvariant()})"));
    }

    private async Task OnSelectedEntryChangedAsync(DictionaryEntryOption? option)
    {
        ViewModel.SelectedEntry = option;

        if (option is not null)
        {
            try
            {
                await ViewModel.MapDictionaryEntryToFormAsync(option.Id, Dispatcher);
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorAsync(ex.Message);
            }
        }
    }

    private async Task GenerateIpaAsync()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Headword))
        {
            ToastService.ShowWarning("Please enter a headword first.");
            return;
        }

        ViewModel.IsGeneratingIpa = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ViewModel.GenerateIpaAsync(Dispatcher);
            if (result.IsFailure)
                ToastService.ShowWarning(result.Error.Message);
        }
        finally
        {
            ViewModel.IsGeneratingIpa = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task GenerateAudioAsync()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Headword))
        {
            ToastService.ShowWarning("Please enter a headword first.");
            return;
        }

        ViewModel.IsGeneratingAudio = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ViewModel.GenerateAudioAsync(Dispatcher);
            if (result.IsFailure)
            {
                ToastService.ShowWarning(result.Error.Message);
                return;
            }

            // 👇 Fix CORS: Google TTS bị block bởi browser, fetch phía server rồi dùng base64 data URL
            try
            {
                using var http = HttpClientFactory.CreateClient();
                var bytes = await http.GetByteArrayAsync(ViewModel.AudioUrl);
                ViewModel.AudioPreviewUrl = $"data:audio/mpeg;base64,{Convert.ToBase64String(bytes)}";
            }
            catch
            {
                // fallback: giữ nguyên AudioUrl, có thể vẫn không play được do CORS
            }
        }
        finally
        {
            ViewModel.IsGeneratingAudio = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadMoreImagesAsync()
    {
        ViewModel.IsLoadingMoreImages = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ViewModel.LoadMoreImagesAsync(Dispatcher);
            if (result.IsFailure)
                ToastService.ShowInfo(result.Error.Message);
        }
        finally
        {
            ViewModel.IsLoadingMoreImages = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task HandleChangeAudioAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;

        var tempPath = Path.GetTempFileName();
        await using var fs = new FileStream(tempPath, FileMode.Create);
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(fs);

        await ViewModel.ProcessUploadedAudioAsync(tempPath, file.Name, file.ContentType);
    }

    protected async Task HandleChangeImageAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;

        var tempPath = Path.GetTempFileName();
        await using var fs = new FileStream(tempPath, FileMode.Create);
        await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(fs);

        await ViewModel.ProcessUploadedImageAsync(tempPath, file.Name, file.ContentType);
    }

    private async Task SubmitAsync()
    {
        if (!ViewModel.EditContext.Validate())
            return;

        ViewModel.IsSubmitting = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ViewModel.SaveAsync(Dispatcher);
            if (result.IsFailure)
            {
                await DialogService.ShowErrorAsync(result.Error.Message);
                return;
            }

            ToastService.ShowSuccess("Learning item created successfully.");
            ViewModel.ClearForm();
        }
        finally
        {
            ViewModel.IsSubmitting = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
