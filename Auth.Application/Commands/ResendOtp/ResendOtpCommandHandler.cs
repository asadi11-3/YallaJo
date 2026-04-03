using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResendOtp;

public sealed class ResendOtpCommandHandler(
    ISecurityService securityService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    IEmailService emailService)
    : ICommandHandler<ResendOtpCommand, ResendOtpResult>
{
    private const string GenericMessage = "If this email exists, a new code was sent.";

    public async Task<Result<ResendOtpResult>> Handle(
        ResendOtpCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, cancellationToken);
        if (userId is null)
            return Result<ResendOtpResult>.Success(new ResendOtpResult(GenericMessage));

        // Rate limit: check if an active unused OTP exists and was created less than 1 minute ago
        var recentOtp = await otpRepository.FirstOrDefaultAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == request.Purpose
                      && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: true,
            ct: cancellationToken);

        if (recentOtp is not null && recentOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
        {
            return Result<ResendOtpResult>.Fail(
               Outcome.TooManyRequests,
               "Please wait before requesting a new code.");
        }

        var oldOtps = await otpRepository.GetAllAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == request.Purpose
                      && !o.IsUsed,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var old in oldOtps)
            old.MarkUsed();

        var plainOtp = otpService.Generate();
        var otpHash = otpService.Hash(plainOtp);

        var otp = Otp.Create(
            userId: userId.Value,
            purpose: request.Purpose,
            codeHash: otpHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail);

        await otpRepository.AddAsync(otp, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var subject = request.Purpose == "PasswordReset"
            ? "YallaJo — Reset Your Password"
            : "YallaJo — Verify Your Email";

        await emailService.SendAsync(
            normalizedEmail,
            subject,
            $"Your verification code is: {plainOtp}. It expires in 10 minutes.",
            cancellationToken);

        return Result<ResendOtpResult>.Success(new ResendOtpResult(GenericMessage));
    }
}
