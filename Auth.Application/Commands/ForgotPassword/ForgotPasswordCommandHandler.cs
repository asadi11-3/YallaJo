using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    ISecurityService securityService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    IEmailService emailService)
    : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private const string GenericMessage = "If this email exists, a reset code was sent.";
    private const string OtpPurpose = "PasswordReset";

    public async Task<Result<ForgotPasswordResult>> Handle(
        ForgotPasswordCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, ct);
        if (userId is null)
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));

        var plainOtp = otpService.Generate();
        var otpHash = otpService.Hash(plainOtp);

        var otp = Otp.Create(
            userId: userId.Value,
            purpose: OtpPurpose,
            codeHash: otpHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail);

        await otpRepository.AddAsync(otp, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await emailService.SendAsync(
            normalizedEmail,
            "YallaJo — Reset Your Password",
            $"Your password reset code is: {plainOtp}. It expires in 10 minutes.",
            ct);

        return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
    }
}
