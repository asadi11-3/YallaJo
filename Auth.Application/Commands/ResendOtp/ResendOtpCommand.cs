using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ResendOtp;

public sealed record ResendOtpResult(string Message);

public sealed record ResendOtpCommand(
    string Email,
    string Purpose) : ICommand<ResendOtpResult>;
