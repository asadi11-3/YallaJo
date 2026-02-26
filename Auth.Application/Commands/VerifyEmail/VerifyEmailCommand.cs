using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.VerifyEmail;

public sealed record VerifyEmailResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record VerifyEmailCommand(string Email, string OtpCode) : ICommand<VerifyEmailResult>;
