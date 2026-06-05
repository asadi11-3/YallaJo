namespace YallaJo.Web.Areas.Creator.Shared;

/// <summary>
/// Lightweight view model for the creator dashboard chrome (avatar card + nav).
/// Mirrors <c>ProviderSidebarVm</c>. Carries only presentation data — no DTOs.
/// </summary>
public sealed class CreatorSidebarVm
{
    public string? AvatarUrl { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }
}
