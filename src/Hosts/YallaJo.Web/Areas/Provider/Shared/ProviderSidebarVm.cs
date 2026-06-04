namespace YallaJo.Web.Areas.Provider.Shared;

/// <summary>
/// Lightweight view model for the provider dashboard chrome (avatar card + nav).
/// </summary>
public sealed class ProviderSidebarVm
{
    public string? AvatarUrl { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }
}
