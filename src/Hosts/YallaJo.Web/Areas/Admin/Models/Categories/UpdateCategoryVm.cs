using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Categories;

public sealed class UpdateCategoryVm
{
    [Required] public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(200)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Display(Name = "Parent category")]
    public Guid? ParentCategoryId { get; set; }

    [StringLength(100)]
    [Display(Name = "Icon")]
    public string? Icon { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Sort order")]
    public int? SortOrder { get; set; }

    [StringLength(10)]
    [Display(Name = "Source language code")]
    public string? SourceLanguageCode { get; set; } = "en";
}
