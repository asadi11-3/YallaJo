using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.ViewModels;

public sealed class TranslateOnDemandVm
{
    [Required] [StringLength(2000)]
    [Display(Name = "Text")]
    public string Text { get; set; } = string.Empty;

    [Required] [StringLength(10)]
    [Display(Name = "From language code")]
    public string FromLanguageCode { get; set; } = "en";

    [Required] [StringLength(10)]
    [Display(Name = "To language code")]
    public string ToLanguageCode { get; set; } = string.Empty;

    public string? LastOriginal { get; set; }
    public string? LastTranslated { get; set; }
    public double? LastConfidence { get; set; }
}
