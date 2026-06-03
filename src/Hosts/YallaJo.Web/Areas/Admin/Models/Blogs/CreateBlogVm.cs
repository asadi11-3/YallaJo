using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Blogs;

public sealed class CreateBlogVm
{
    [Required]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Source language code is required (e.g. \"en\").")]
    [Display(Name = "Source language code")]
    [StringLength(10)]
    public string SourceLanguageCode { get; set; } = "en";

    [StringLength(300, ErrorMessage = "Slug cannot exceed 300 characters.")]
    public string? Slug { get; set; }

    public string? Summary { get; set; }

    [Display(Name = "Meta title")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta description")]
    public string? MetaDescription { get; set; }

    [Display(Name = "Place ID (optional)")]
    public Guid? PlaceId { get; set; }
}
