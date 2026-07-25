using AnkiBridge.Application.Features.Flashcard.UseCases.ExportNotes;
using AnkiBridge.Application.Features.Flashcard.UseCases.SearchNotes;
using AnkiBridge.Domain.Enums;
using AnkiBridge.Shared.Extensions;
using AnkiBridge.Web.Features.Flashcard.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AnkiBridge.Web.Features.Flashcard.Pages;

public partial class NoteList
{
    private string Headword { get; set; } = string.Empty;
    private string NoteType { get; set; } = string.Empty;
    private string Deck { get; set; } = string.Empty;
    private ExportStatus? Status { get; set; }

    private bool IsExporting { get; set; }

    private async Task HandleTabChangeAsync(FluentTab tab)
    {
        if (tab.Id is null || !Enum.TryParse<AnkiNoteExportStatus>(tab.Id, out var status))
            return;

        Status = status switch
        {
            AnkiNoteExportStatus.All => null,
            AnkiNoteExportStatus.NotStarted => ExportStatus.NotStarted,
            AnkiNoteExportStatus.Processing => ExportStatus.Processing,
            AnkiNoteExportStatus.Success => ExportStatus.Success,
            AnkiNoteExportStatus.Failed => ExportStatus.Failed,
            _ => Status
        };

        SelectedGridItemIds.Clear();
        await RefreshDataAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        SelectedGridItemIds.Clear();
        await RefreshDataAsync();
    }

    private async Task ClearFiltersAsync()
    {
        Headword = string.Empty;
        NoteType = string.Empty;
        Deck = string.Empty;
        SelectedGridItemIds.Clear();
        await RefreshDataAsync();
    }

    protected override async Task RefreshItemsAsync(GridItemsProviderRequest<NoteGridItem> request)
    {
        if (request.Count is null)
            return;

        IsRefreshing = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var query = new SearchNotesQuery
            {
                Headword = Headword,
                NoteType = NoteType,
                Deck = Deck,
                Status = Status,
                PageNumber = request.StartIndex / request.Count.Value + 1,
                PageSize = request.Count.Value
            };

            var result = await Dispatcher.Send(query);

            await result.Match(
                async data =>
                {
                    GridItems = data.Items
                        .Select(x => new NoteGridItem
                        {
                            Id = x.Id,
                            Headword = x.Headword,
                            Deck = x.Deck,
                            NoteType = x.NoteType,
                            ExportStatus = x.ExportStatus,
                            CreatedDate = x.CreatedAt
                        })
                        .AsQueryable();

                    await pagination.SetTotalItemCountAsync(data.TotalCount);
                },
                error =>
                {
                    DialogService.ShowError(error.Message);
                    return Task.CompletedTask;
                });
        }
        finally
        {
            IsRefreshing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ExportToAnkiAsync()
    {
        IsExporting = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            await Dispatcher
                .Send(new ExportNotesCommand(SelectedGridItemIds.ToList()))
                .Match(
                    async () =>
                    {
                        SelectedGridItemIds.Clear();
                        ToastService.ShowSuccess("Export started. Refresh the list to monitor its status.");
                        await RefreshDataAsync();
                    },
                    error => DialogService.ShowErrorAsync(error.Message));
        }
        finally
        {
            IsExporting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public enum AnkiNoteExportStatus
    {
        All,
        NotStarted,
        Processing,
        Success,
        Failed
    }
}
