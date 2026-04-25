using Accounts.Contracts.Abstractions;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRegistrationService userRegistrationService,
    IProfileCreationService  profileCreationService,
    IOtpRepository           otpRepository,
    IAuthUnitOfWork          unitOfWork,
    IOtpService              otpService,
    IEmailService            emailService,
    ILogger<RegisterCommandHandler> logger)
    : ICommandHandler<RegisterCommand, RegisterResult>
{
    private const string EmailVerificationPurpose = "EmailVerification";
    private const int    OtpExpiryMinutes         = 10;

    public async Task<Result<RegisterResult>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var userResult = await userRegistrationService.RegisterAsync(
            new UserRegistrationRequest(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password),
            cancellationToken);

        if (userResult.IsFailure)
        {
            return Result<RegisterResult>.Fail(
                userResult.Outcome,
                userResult.Messages.FirstOrDefault() ?? string.Empty,
                userResult.Errors.ToArray());
        }

        var userId = userResult.Value;

        var profileResult = await profileCreationService.CreateForUserAsync(
            new ProfileCreationRequest(
                userId,
                request.FirstName,
                request.LastName),
            cancellationToken);

        if (profileResult.IsFailure && profileResult.Outcome != Outcome.Conflict)
        {
            return Result<RegisterResult>.Fail(
                profileResult.Outcome,
                profileResult.Messages.FirstOrDefault() ?? string.Empty,
                profileResult.Errors.ToArray());
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var plainOtp        = otpService.Generate();
        var otpHash         = otpService.Hash(plainOtp);

        var otp = Otp.Create(
            userId:          userId,
            purpose:         EmailVerificationPurpose,
            codeHash:        otpHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail,
            expiryMinutes:   OtpExpiryMinutes);

        await otpRepository.AddAsync(otp, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendAsync(
                normalizedEmail,
                "YallaJo — Verify Your Email",
                $"Your verification code is: {plainOtp}\n\nThis code expires in {OtpExpiryMinutes} minutes.",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            otp.MarkUsed();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogError(ex,
                "Auth: Failed to send verification OTP to {Email} during registration. OTP invalidated; user can request resend.",
                normalizedEmail);

            return Result<RegisterResult>.Failure(
                Error.Failure(
                    "Registration.EmailDeliveryFailed",
                    "Account created but we couldn't send the verification email. Please use Resend Code."),
                Outcome.ServerError);
        }

        return Result<RegisterResult>.Created(new RegisterResult(userId));
    }
}
