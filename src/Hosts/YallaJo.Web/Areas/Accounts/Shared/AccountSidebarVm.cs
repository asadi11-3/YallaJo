namespace YallaJo.Web.Areas.Accounts.Shared;

public sealed class AccountSidebarVm
{
    public string? AvatarUrl { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
