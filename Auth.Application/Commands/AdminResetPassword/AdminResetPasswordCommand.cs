using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AdminResetPassword;

public sealed record AdminResetPasswordCommand(
    Guid TargetUserId,
    string? Reason) : ICommand<AdminResetPasswordResult>;
