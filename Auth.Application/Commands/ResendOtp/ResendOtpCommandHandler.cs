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
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, ct);
        if (userId is null)
            return Result<ResendOtpResult>.Success(new ResendOtpResult(GenericMessage));

        // Rate limit: check if an active unused OTP exists and was created less than 1 minute ago
        var recentOtp = await otpRepository.FirstOrDefaultAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == request.Purpose
                      && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: true,
            ct: ct);

        if (recentOtp is not null && recentOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
            return Result<ResendOtpResult>.Fail(
                Outcome.TooManyRequests,
                "Please wait before requesting a new code.");

        // Mark old active OTPs for this purpose as used
        var oldOtps = await otpRepository.GetAllAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == request.Purpose
                      && !o.IsUsed,
            asNoTracking: false,
            ct: ct);

        foreach (var old in oldOtps)
            old.MarkUsed();

        // Generate new OTP
        var plainOtp = otpService.Generate();
        var otpHash = otpService.Hash(plainOtp);

        var otp = Otp.Create(
            userId: userId.Value,
            purpose: request.Purpose,
            codeHash: otpHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail);

        await otpRepository.AddAsync(otp, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var subject = request.Purpose == "PasswordReset"
            ? "YallaJo — Reset Your Password"
            : "YallaJo — Verify Your Email";

        await emailService.SendAsync(
            normalizedEmail,
            subject,
            $"Your verification code is: {plainOtp}. It expires in 10 minutes.",
            ct);

        return Result<ResendOtpResult>.Success(new ResendOtpResult(GenericMessage));
    }
}
