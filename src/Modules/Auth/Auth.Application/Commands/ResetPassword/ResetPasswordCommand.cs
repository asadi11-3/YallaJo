using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string OtpCode,
    string NewPassword,
    string ConfirmNewPassword) : ICommand<ResetPasswordResult>;
