namespace YallaJo.Web.Areas.Auth.Models.ResetPassword;

public sealed class ResetPasswordRequest
{
    public string Email              { get; init; } = string.Empty;
    public string OtpCode            { get; init; } = string.Empty;
    public string NewPassword        { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
