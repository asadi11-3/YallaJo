using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Translations;

public sealed class UpdateTranslationVm
{
    [Required] public Guid Id { get; set; }

    [Required(ErrorMessage = "Translated text is required.")]
    [StringLength(4000)]
    [Display(Name = "Translated text")]
    public string TranslatedText { get; set; } = string.Empty;
}
