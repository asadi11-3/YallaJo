using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : ICommand<ChangePasswordResult>;
