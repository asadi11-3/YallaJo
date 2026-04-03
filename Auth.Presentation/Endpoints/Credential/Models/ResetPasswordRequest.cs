namespace Auth.Presentation.Endpoints.Credential.Models;

public sealed record ResetPasswordRequest(
    string Email,
    string OtpCode,
    string NewPassword,
    string ConfirmNewPassword);
