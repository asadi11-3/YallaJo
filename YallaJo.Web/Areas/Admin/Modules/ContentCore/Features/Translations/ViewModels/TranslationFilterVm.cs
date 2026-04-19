using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.ViewModels;

public sealed class TranslationFilterVm
{
    [Required]
    [Display(Name = "Entity type")]
    public EntityTypeOption EntityType { get; set; }

    [Required]
    [Display(Name = "Entity ID")]
    public Guid EntityId { get; set; }
}
