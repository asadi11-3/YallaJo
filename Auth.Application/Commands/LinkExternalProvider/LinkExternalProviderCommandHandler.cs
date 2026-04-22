using Auth.Application.ExternalAuth;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.LinkExternalProvider;

/// <summary>
/// Links an external-provider identity (Google, Facebook, …) to the CURRENT
/// authenticated user.
///
/// <para>Security properties enforced by this handler:</para>
/// <list type="bullet">
///   <item><description>Caller must be authenticated — linking without a verified
///   user context is never allowed.</description></item>
///   <item><description>Only a verified, single-use, signed ticket is accepted.
///   Raw provider IDs supplied by the client are rejected.</description></item>
///   <item><description>Ticket nonce is consumed atomically BEFORE any mutation
///   so that replay attempts cannot race a successful link.</description></item>
///   <item><description>A provider identity cannot be linked to more than one user
///   (enforced in-code + by a filtered unique DB index).</description></item>
///   <item><description>The same user cannot active-link the same provider twice
///   (prevents runaway row growth on repeated link clicks).</description></item>
///   <item><description>Relinking after unlink is allowed — previous row stays
///   inactive for audit and the filtered index permits insert.</description></item>
/// </list>
/// </summary>
public sealed class LinkExternalProviderCommandHandler(
    IExternalProviderRepository externalProviderRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IExternalAuthTicketVerifier ticketVerifier,
    IExternalAuthNonceStore nonceStore,
    ILogger<LinkExternalProviderCommandHandler> logger)
    : ICommandHandler<LinkExternalProviderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(LinkExternalProviderCommand request, CancellationToken ct)
    {
        // 1. Authenticated context required.
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<Guid>.Failure(
                Error.Unauthorized("Authentication is required to link an external provider."),
                Outcome.Unauthorized);
        }

        var userId = currentUser.UserId.Value;

        // 2. Verify the signed ticket. Rejects tampering, expiry, wrong provider,
        //    missing claims. The ticket carries the trusted provider identity.
        var verification = ticketVerifier.Verify(request.Ticket);
        if (verification.IsFailure)
        {
            logger.LogWarning(
                "External provider link rejected for user {UserId}: invalid ticket ({Error})",
                userId, verification.Error?.Message);

            return Result<Guid>.Failure(
                verification.Error ?? Error.Unauthorized("Invalid external provider ticket."),
                Outcome.Unauthorized);
        }

        var ticket = verification.Value!;
        var normalizedProvider = ticket.Provider.Trim().ToLowerInvariant();

        // 3. Consume the ticket nonce ONCE. Replay = reject.
        var consumed = await nonceStore.TryConsumeAsync(ticket.TicketId, ticket.ExpiresAt, ct);
        if (!consumed)
        {
            logger.LogWarning(
                "External provider link rejected for user {UserId}: ticket {TicketId} already consumed (replay).",
                userId, ticket.TicketId);

            return Result<Guid>.Failure(
                Error.Unauthorized("This external-provider authentication has already been used."),
                Outcome.Unauthorized);
        }

        // 4. Guard: this user already has an active link for this provider.
        var alreadyLinked = await externalProviderRepository.AnyAsync(
            ep => ep.UserId == userId
               && ep.Provider == normalizedProvider
               && ep.IsActive,
            ct);

        if (alreadyLinked)
        {
            return Result<Guid>.Conflict(
                Error.Conflict(
                    "ExternalProvider.AlreadyLinked",
                    $"A {ticket.Provider} account is already linked to your profile."));
        }

        // 5. Guard: this provider identity is not already claimed by another user.
        var takenByOther = await externalProviderRepository.AnyAsync(
            ep => ep.ProviderUserId == ticket.ProviderUserId
               && ep.Provider == normalizedProvider
               && ep.IsActive,
            ct);

        if (takenByOther)
        {
            return Result<Guid>.Conflict(
                Error.Conflict(
                    "ExternalProvider.ProviderIdTaken",
                    $"This {ticket.Provider} account is already linked to another user."));
        }

        var externalProvider = ExternalProvider.Create(
            userId: userId,
            provider: normalizedProvider,
            providerUserId: ticket.ProviderUserId,
            providerEmail: ticket.Email);

        await externalProviderRepository.AddAsync(externalProvider, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // NOTE: A concurrent insert that races past the in-code guard will violate
        // the filtered unique index `IX_ExternalProviders_Provider_ProviderUserId_Active`
        // — the host's global DbUpdateExceptionHandler maps that to a 409 Conflict
        // response, so we do not need EF-specific try/catch here.

        return Result<Guid>.Success(externalProvider.Id);
    }
}
