namespace YallaJo.Web.Areas.Business.Shared;

public sealed class BusinessSidebarVm
{
    public string? AvatarUrl { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }

    /// <summary>
    /// The currently selected business id (if any). Drives the per-business
    /// sub-navigation (hours, amenities, services, staff, accessibility).
    /// </summary>
    public Guid? BusinessId { get; set; }

    public bool HasBusiness => BusinessId is { } id && id != Guid.Empty;
}
