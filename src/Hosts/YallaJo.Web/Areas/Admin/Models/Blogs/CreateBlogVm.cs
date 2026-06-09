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

    /// <summary>
    /// Optional related place. Rendered as a name dropdown (F10: never a raw GUID
    /// textbox) whose option values carry the place id while the user only ever
    /// reads the place name.
    /// </summary>
    public Guid? PlaceId { get; set; }

    /// <summary>Selectable places (name shown, id submitted). Populated by the facade.</summary>
    public IReadOnlyList<PlaceOptionVm> PlaceOptions { get; set; } = [];
}

/// <summary>A selectable place for the related-place dropdown (F10: name shown, id is the value).</summary>
public sealed class PlaceOptionVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
