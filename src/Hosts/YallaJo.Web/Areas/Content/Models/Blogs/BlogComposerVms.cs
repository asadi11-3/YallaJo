using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Content.Models.Blogs;

public class CreateBlogVm
{
    [Required(ErrorMessage = "Please enter a title.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be 3-200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write some content.")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Language")]
    public string SourceLanguageCode { get; set; } = "en";

    [StringLength(160)]
    [Display(Name = "URL slug")]
    public string? Slug { get; set; }

    [StringLength(500)]
    public string? Summary { get; set; }

    [StringLength(200)]
    [Display(Name = "Meta title")]
    public string? MetaTitle { get; set; }

    [StringLength(320)]
    [Display(Name = "Meta description")]
    public string? MetaDescription { get; set; }
}

public sealed class EditBlogVm : CreateBlogVm
{
    public Guid Id { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public string? Status { get; set; }
}

public sealed class MyBlogsVm
{
    public IReadOnlyList<MyBlogRowVm> Blogs { get; init; } = [];

    public bool HasBlogs => Blogs.Count > 0;
}

public sealed class MyBlogRowVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }

    public bool CanSubmit => string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase);
}
