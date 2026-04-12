namespace YallaJo.Web.Areas.Auth.Features.VerifyEmail.Requests;

/// <summary>Outbound payload for POST /api/v1/auth/verify-email.</summary>
public sealed class VerifyEmailRequest
{
    public string Email   { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
}
