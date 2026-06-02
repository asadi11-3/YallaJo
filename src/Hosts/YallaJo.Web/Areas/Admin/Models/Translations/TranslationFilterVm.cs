using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Translations;

public sealed class TranslationFilterVm
{
    [Required]
    [Display(Name = "Entity type")]
    public EntityTypeOption EntityType { get; set; }

    [Required]
    [Display(Name = "Entity ID")]
    public Guid EntityId { get; set; }
}
