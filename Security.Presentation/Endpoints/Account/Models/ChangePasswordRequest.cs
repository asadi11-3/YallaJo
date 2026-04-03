namespace Security.Presentation.Endpoints.Account.Models;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
