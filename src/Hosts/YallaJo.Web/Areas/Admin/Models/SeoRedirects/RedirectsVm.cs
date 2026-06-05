using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.SeoRedirects;

public sealed class RedirectsVm
{
    public IReadOnlyList<RedirectRowVm> Redirects { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? OldUrlFilter { get; set; }
    public bool? IsActiveFilter { get; set; }

    public bool HasRedirects => Redirects.Count > 0;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class RedirectRowVm
{
    public Guid Id { get; set; }
    public string OldUrl { get; set; } = "";
    public string NewUrl { get; set; } = "";
    public int StatusCode { get; set; }
    public bool IsActive { get; set; }
    public long HitCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateRedirectFormVm
{
    [Required]
    [StringLength(2048, MinimumLength = 1)]
    [Display(Name = "Old URL")]
    public string OldUrl { get; set; } = "";

    [Required]
    [StringLength(2048, MinimumLength = 1)]
    [Display(Name = "New URL")]
    public string NewUrl { get; set; } = "";

    [Range(300, 399)]
    [Display(Name = "Status code")]
    public int StatusCode { get; set; } = 301;
}

public sealed class UpdateRedirectFormVm
{
    [Required]
    public Guid Id { get; set; }

    [StringLength(2048)]
    [Display(Name = "New URL")]
    public string? NewUrl { get; set; }

    [Range(300, 399)]
    [Display(Name = "Status code")]
    public int? StatusCode { get; set; }

    [Display(Name = "Active")]
    public bool? IsActive { get; set; }
}
