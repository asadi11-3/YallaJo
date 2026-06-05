using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;

namespace YallaJo.Web.Areas.Provider.Models.Settings;

public sealed class ProviderSettingsVm
{
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? PhoneNumber { get; init; }

    public UpdateProfileVm Profile { get; set; } = new();
    public ChangePasswordVm Password { get; set; } = new();
    public UpdatePhoneVm Phone { get; set; } = new();

    /// <summary>
    /// Business information from <c>GET /api/v1/provider/settings</c>. Null when the
    /// provider has no application yet or the call fails (rendered conditionally).
    /// </summary>
    public ProviderBusinessInfoVm? Business { get; set; }
}

public sealed class ProviderBusinessInfoVm
{
    public string BusinessName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty;
}
