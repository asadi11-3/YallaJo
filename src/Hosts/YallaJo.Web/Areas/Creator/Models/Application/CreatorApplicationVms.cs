using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Creator.Models.Application;

public sealed class CreatorApplicationFormVm
{
    /// <summary>Existing application id when editing; null for first-time create.</summary>
    public Guid? ApplicationId { get; set; }

    /// <summary>Raw backend status ("Draft" | "Pending" | "Approved" | "Rejected" | "MoreInfoNeeded"); null when none.</summary>
    public string? Status { get; set; }

    /// <summary>Reviewer note shown for Rejected / MoreInfoNeeded.</summary>
    public string? AdminNote { get; set; }

    [StringLength(2000, ErrorMessage = "Bio must be 2000 characters or fewer.")]
    public string? Bio { get; set; }

    /// <summary>Newline-separated portfolio URLs (max 10).</summary>
    [Display(Name = "Portfolio URLs")]
    public string? PortfolioUrlsText { get; set; }

    /// <summary>Newline-separated sample-work URLs (max 10).</summary>
    [Display(Name = "Sample work URLs")]
    public string? SampleWorkUrlsText { get; set; }

    /// <summary>Comma-separated free tags (max 20, each ≤ 50 chars).</summary>
    [Display(Name = "Tags")]
    public string? FreeTagsText { get; set; }

    /// <summary>Selected niche ids (max 5).</summary>
    [Display(Name = "Niches")]
    public List<Guid> SelectedNicheIds { get; set; } = [];

    /// <summary>All active niches to render as checkboxes (re-populated on redisplay).</summary>
    public IReadOnlyList<CreatorNicheOptionVm> NicheOptions { get; set; } = [];

    public bool IsEditable =>
        Status is null
        || string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "MoreInfoNeeded", StringComparison.OrdinalIgnoreCase);

    public bool IsNew => ApplicationId is null;

    public bool CanSubmit =>
        ApplicationId is not null
        && string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase);
}

public sealed class CreatorNicheOptionVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
public sealed class RedeemInvitationVm
{
    [Required(ErrorMessage = "Please enter your invitation token.")]
    [StringLength(100, ErrorMessage = "Token must be 100 characters or fewer.")]
    [Display(Name = "Invitation token")]
    public string Token { get; set; } = string.Empty;
}
