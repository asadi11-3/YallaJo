namespace YallaJo.Web.Areas.Accounts.Features.ChangePassword.Requests;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
