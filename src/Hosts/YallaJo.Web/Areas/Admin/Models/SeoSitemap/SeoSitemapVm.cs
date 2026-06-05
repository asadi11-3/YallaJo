using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.SeoSitemap;

public sealed class SeoSitemapVm
{
    public IReadOnlyList<SitemapEntryRowVm> Entries { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? EntityTypeFilter { get; set; }
    public bool? IsActiveFilter { get; set; }

    public bool HasEntries => Entries.Count > 0;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class SitemapEntryRowVm
{
    public Guid Id { get; set; }
    public string Url { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public decimal? Priority { get; set; }
    public string? ChangeFrequency { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UpdateSitemapEntryFormVm
{
    [Required]
    public Guid Id { get; set; }

    [Range(0.0, 1.0)]
    [Display(Name = "Priority")]
    public decimal? Priority { get; set; }

    [StringLength(20)]
    [Display(Name = "Change frequency")]
    public string? ChangeFrequency { get; set; }
}
