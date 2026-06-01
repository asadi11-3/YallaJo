using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    ISecurityService securityService,
    IPasswordResetTokenRepository resetTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ILogger<ForgotPasswordCommandHandler> logger)
    : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private const string GenericMessage  = "If this email exists, a reset code was sent.";
    private const int    ThrottleSeconds = 60;
    private const int    ExpiryMinutes   = 10;

    public async Task<Result<ForgotPasswordResult>> Handle(
        ForgotPasswordCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var status = await securityService.GetAccountStatusByEmailAsync(normalizedEmail, ct);
        if (status is null
         || status.Lifecycle != AccountLifecycleSnapshot.Active
         || !status.IsEmailVerified)
        {
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
        }

        var userId = status.UserId;

        var recent = await resetTokenRepository.GetLatestActiveForUserReadOnlyAsync(userId, ct);
        if (recent is not null
            && recent.CreatedAt > DateTime.UtcNow.AddSeconds(-ThrottleSeconds))
        {
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
        }

        var plainCode = otpService.Generate();
        var codeHash  = otpService.Hash(plainCode);

        var active = await resetTokenRepository.GetActiveForUserAsync(userId, ct);
        foreach (var prior in active)
            prior.Supersede();

        var token = PasswordResetToken.Issue(
            userId:          userId,
            tokenHash:       codeHash,
            deliveryAddress: normalizedEmail,
            expiryMinutes:   ExpiryMinutes,
            origin:          PasswordResetOrigin.SelfService);

        token.AddDomainEvent(new PasswordResetTokenIssuedEvent(
            TokenId:         token.Id,
            UserId:          token.UserId,
            DeliveryAddress: token.DeliveryAddress,
            PlainCode:       plainCode,
            ExpiresAt:       token.ExpiresAt,
            Origin:          token.ResetOrigin));

        await resetTokenRepository.AddAsync(token, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ForgotPasswordResult>.Failure(
                new Error("PasswordResetToken.ConcurrencyConflict", "Password reset state was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        logger.LogInformation(
            "Auth: PasswordResetToken {TokenId} issued for user {UserId}; email queued on outbox.",
            token.Id, userId);

        return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
    }
}
