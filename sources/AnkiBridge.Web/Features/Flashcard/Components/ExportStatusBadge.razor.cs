using AnkiBridge.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace AnkiBridge.Web.Features.Flashcard.Components;

public partial class ExportStatusBadge
{
    [Parameter]
    public ExportStatus Status { get; set; }

    private string CssClass => Status switch
    {
        ExportStatus.Processing => "export-status export-status--processing",
        ExportStatus.Success => "export-status export-status--success",
        ExportStatus.Failed => "export-status export-status--failed",
        ExportStatus.Cancelled => "export-status export-status--cancelled",
        _ => "export-status"
    };
}
