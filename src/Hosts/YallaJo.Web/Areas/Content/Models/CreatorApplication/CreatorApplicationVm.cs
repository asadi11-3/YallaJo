using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using YallaJo.Web.Areas.Content.Models.Blogs;

namespace YallaJo.Web.Areas.Content.Models.CreatorApplication;

/// <summary>
/// The "apply to become a creator" page: an editable form plus the niche choices.
/// </summary>
public sealed class CreatorApplyVm
{
    public CreatorApplicationFormVm Form { get; init; } = new();
    public IReadOnlyList<NicheChoiceVm> Niches { get; init; } = [];
    public bool HasNiches => Niches.Count > 0;
}

/// <summary>
/// The "my application" page: current status plus the editable form (when still editable).
/// </summary>
public sealed class CreatorApplicationStatusVm
{
    public bool HasApplication { get; init; }
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string? AdminNote { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>Draft and MoreInfoNeeded applications can still be edited and submitted.</summary>
    public bool IsEditable { get; init; }

    /// <summary>Draft and MoreInfoNeeded applications can be submitted for review.</summary>
    public bool CanSubmit { get; init; }

    public CreatorApplicationFormVm Form { get; set; } = new();
    public IReadOnlyList<NicheChoiceVm> Niches { get; init; } = [];
}

public sealed class CreatorApplicationFormVm
{
    public Guid Id { get; set; }

    [StringLength(2000)]
    [Display(Name = "About you")]
    public string? Bio { get; set; }

    [StringLength(4000)]
    [Display(Name = "Portfolio links")]
    public string? PortfolioUrls { get; set; }

    [StringLength(4000)]
    [Display(Name = "Sample work links")]
    public string? SampleWorkUrls { get; set; }

    [Display(Name = "Niches")]
    public List<Guid> NicheIds { get; set; } = [];

    [StringLength(1000)]
    [Display(Name = "Tags")]
    public string? FreeTags { get; set; }

    [StringLength(2000)]
    [Display(Name = "Social handles")]
    public string? SocialHandles { get; set; }
}

public sealed class NicheChoiceVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Selected { get; init; }
}

public sealed class RedeemCreatorInvitationVm
{
    [Required]
    [StringLength(512, MinimumLength = 1)]
    [Display(Name = "Invitation token")]
    public string Token { get; set; } = string.Empty;
}
