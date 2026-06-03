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
}
