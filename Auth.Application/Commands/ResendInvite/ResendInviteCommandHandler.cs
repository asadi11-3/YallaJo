using Auth.Application.Commands.SendActivationEmail;
using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResendInvite;

/// <summary>
/// Phase 2B — legacy resend entry point. Retained as a thin, enumeration-safe
/// façade over <see cref="SendActivationEmailCommand"/>:
/// <list type="bullet">
///   <item><description>Account not found → generic success (do not leak existence).</description></item>
///   <item><description>Account past activation (Active / Suspended / Archived / PendingPasswordReset) → surface as <c>Conflict</c>, matching the pre-2B <c>Invite.AlreadyCompleted</c> response the admin UI already handles.</description></item>
///   <item><description>Email delivery failure → generic success (do not leak existence on failure either; admin can check logs/metrics).</description></item>
///   <item><description>Success → generic success message.</description></item>
/// </list>
/// <para>
/// The HTTP endpoint contract (<c>POST /invitations/resend</c>) and response
/// DTO (<see cref="ResendInviteResult"/>) are unchanged.
/// </para>
/// </summary>
public sealed class ResendInviteCommandHandler(IMediator mediator)
    : ICommandHandler<ResendInviteCommand, ResendInviteResult>
{
    private const string GenericMessage =
        "If an invited account exists for this email, a new invite has been sent.";

    public async Task<Result<ResendInviteResult>> Handle(
        ResendInviteCommand request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new SendActivationEmailCommand(request.Email), ct);

        if (result.IsSuccess)
            return Result<ResendInviteResult>.Success(new ResendInviteResult(GenericMessage));

        // Surface Conflict verbatim — the admin UI distinguishes
        // "already completed" from a generic success so the operator can
        // tell when a resend is genuinely refused.
        if (result.Outcome == Outcome.Conflict)
        {
            return Result<ResendInviteResult>.Fail(
                result.Outcome,
                result.Messages.FirstOrDefault() ?? string.Empty,
                result.Errors.ToArray());
        }

        // Every other failure — NotFound (no such account), ServerError
        // (SMTP dropped), anything else — is absorbed into the generic
        // success response to preserve enumeration safety.
        return Result<ResendInviteResult>.Success(new ResendInviteResult(GenericMessage));
    }
}
