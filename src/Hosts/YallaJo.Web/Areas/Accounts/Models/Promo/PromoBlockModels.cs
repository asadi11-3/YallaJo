using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Accounts.Models.Promo;

/// <summary>
/// View model for a single promotional placement rendered on the My Profile page.
/// </summary>
public sealed class PromoBlockVm
{
    public string PlacementKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? IconName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}

/// <summary>
/// Form-bound model for the inline promo editor (Admin/SuperAdmin/Owner only).
/// </summary>
public sealed class UpdatePromoBlockVm
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? ButtonText { get; set; }

    [StringLength(2048)]
    public string? ButtonUrl { get; set; }

    [StringLength(60)]
    public string? BadgeText { get; set; }

    [StringLength(60)]
    public string? IconName { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}

/// <summary>
/// Transport DTO matching the ContentCore API PromoBlockDto JSON shape (for deserialization).
/// </summary>
public sealed class PromoBlockResponse
{
    public Guid Id { get; set; }
    public string PlacementKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? IconName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}

/// <summary>
/// Wraps the API's UpdatePromoBlockResult / SetPromoBlockImageResult shape ({ "promoBlock": { ... } }).
/// </summary>
public sealed class PromoBlockEnvelope
{
    public PromoBlockResponse? PromoBlock { get; set; }
}

/// <summary>
/// Request body sent to the ContentCore API PUT /promo-blocks/{key} endpoint.
/// </summary>
public sealed record UpdatePromoBlockRequest(
    string Title,
    string? Description,
    string? ButtonText,
    string? ButtonUrl,
    string? BadgeText,
    string? IconName,
    bool IsActive,
    int SortOrder,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);
