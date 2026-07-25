using AnkiBridge.Application.Features.Flashcard.UseCases.CreateNotes;
using AnkiBridge.Application.Features.Flashcard.UseCases.SearchNoteTypes;
using AnkiBridge.Application.Features.Flashcard.UseCases.SearchDecks;
using AnkiBridge.Application.Features.Flashcard.UseCases.SyncDecks;
using AnkiBridge.Application.Features.Flashcard.UseCases.SyncNoteTypes;
using AnkiBridge.Shared.Extensions;
using AnkiBridge.Web.Features.Flashcard.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AnkiBridge.Web.Features.Flashcard.Components;

public partial class CreateNotesDialog
{
    [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;

    private EditContext editContext = default!;

    private NoteFormModel Form { get; set; } = new();

    private bool isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        editContext = new EditContext(Form);

        await LoadNoteTypesAsync();
        await LoadDecksAsync();
    }

    private async Task LoadDecksAsync()
    {
        await Dispatcher
            .Send(new SearchDecksQuery
            {
                PageNumber = 1,
                PageSize = 100
            })
            .Match(
                async data =>
                {
                    Form.DeckOptions = data.Items
                        .Select(d => new AnkiOption(d.Id, d.Name))
                        .ToList();

                    if (Form.DeckOptions.Any())
                        Form.SelectedDeck = Form.DeckOptions.First();
                },
                error => DialogService.ShowErrorAsync(error.Message));
    }
    
    private async Task LoadNoteTypesAsync()
    {
        await Dispatcher
            .Send(new SearchNoteTypesQuery
            {
                PageNumber = 1,
                PageSize = 100
            })
            .Match(
                async data =>
                {
                    Form.NoteTypeOptions = data.Items
                        .Select(n => new AnkiOption(n.Id, n.Name))
                        .ToList();

                    if (Form.NoteTypeOptions.Any())
                        Form.SelectedNoteType = Form.NoteTypeOptions.First();
                },
                error => DialogService.ShowErrorAsync(error.Message));
    }

    private async Task SyncDecksAsync()
    {
        await Dispatcher
            .Send(new SyncDecksCommand())
            .Match(
                async () =>
                {
                    ToastService.ShowSuccess("The Anki decks have been synchronized successfully!");
                    await LoadDecksAsync();
                },
                error => DialogService.ShowErrorAsync(error.Message));
    }

    private async Task SyncNoteTypesAsync()
    {
        await Dispatcher
            .Send(new SyncNoteTypesCommand())
            .Match(
                async () =>
                {
                    ToastService.ShowSuccess("The Anki note types have been synchronized successfully!");
                    await LoadNoteTypesAsync();
                },
                error => DialogService.ShowErrorAsync(error.Message));
    }

    private async Task OnNoteTypeChangedAsync(AnkiOption option)
    {
        Form.SelectedNoteType = option;
    }

    private async Task OnDeckChangedAsync(AnkiOption option)
    {
        Form.SelectedDeck = option;
    }

    private async Task SubmitAsync()
    {
        if (isSubmitting || !editContext.Validate())
            return;

        isSubmitting = true;

        try
        {
            await Dispatcher
                .Send(new CreateNotesCommand(
                    LearningEntryIds: Content.LearningEntryIds,
                    NoteTypeId: Form.SelectedNoteType.Id,
                    DeckId: Form.SelectedDeck.Id))
                .Match(
                    async _ =>
                    {
                        await Dialog.CloseAsync();
                        ToastService.ShowSuccess("Anki notes created successfully.");
                    },
                    error => DialogService.ShowErrorAsync(error.Message));
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();
}
