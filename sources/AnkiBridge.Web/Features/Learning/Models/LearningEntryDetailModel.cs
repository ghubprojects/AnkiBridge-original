using AnkiBridge.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AnkiBridge.Web.Features.Learning.Models;

public sealed class LearningEntryDetailModel
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Headword { get; set; } = string.Empty;

    [Required]
    public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;

    [Required]
    public Accent Accent { get; set; } = Accent.American;

    [Required]
    public string Ipa { get; set; } = string.Empty;

    [Required]
    public string Cloze { get; set; } = string.Empty;

    [Required]
    public string Definition { get; set; } = string.Empty;

    [Required]
    public string Translation { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = [string.Empty];
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
}
