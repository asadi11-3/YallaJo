using Auth.Application.Commands.ProvisionAccount;
using Auth.Application.Commands.SendActivationEmail;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.InviteUser;

/// <summary>
/// Phase 2B — legacy invite entry point. Retained as a thin façade over the
/// two explicit use cases it originally fused together:
/// <list type="number">
///   <item><description><see cref="ProvisionAccountCommand"/> — creates Security identity + Accounts profile shell in <c>Provisioned</c> state.</description></item>
///   <item><description><see cref="SendActivationEmailCommand"/> — issues an activation token, sends the activation email, and transitions the account to <c>PendingActivation</c>.</description></item>
/// </list>
/// <para>
/// Failure handling (preserved from pre-2B behaviour, per enterprise model):
/// if provisioning succeeds but activation-email delivery fails, the
/// provisioned user + profile are NOT rolled back. The admin can retry via
/// <c>ResendInviteCommand</c> (or directly via <see cref="SendActivationEmailCommand"/>).
/// Provisioning-without-activation IS a valid state in the target business
/// model — an admin may provision an account weeks before the real person
/// is ready.
/// </para>
/// <para>
/// The HTTP endpoint contract (<c>POST /invitations</c>) and response DTO
/// (<see cref="InviteUserResult"/>) are unchanged.
/// </para>
/// </summary>
public sealed class InviteUserCommandHandler(
    IMediator mediator,
    ILogger<InviteUserCommandHandler> logger)
    : ICommandHandler<InviteUserCommand, InviteUserResult>
{
    public async Task<Result<InviteUserResult>> Handle(
        InviteUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Provision — admin identity + profile in Provisioned state.
        var provision = await mediator.Send(
            new ProvisionAccountCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.DisplayName,
                request.AvatarUrl,
                request.InitialRoleIds),
            cancellationToken);

        if (provision.IsFailure)
        {
            return Result<InviteUserResult>.Fail(
                provision.Outcome,
                provision.Messages.FirstOrDefault() ?? string.Empty,
                provision.Errors.ToArray());
        }

        var userId    = provision.Value.UserId;
        var profileId = provision.Value.ProfileId;

        // 2. Send activation email — transitions to PendingActivation on
        //    success. On failure we surface the error so the admin knows
        //    to retry, but do NOT roll back the provisioned user/profile.
        //    See handler XML doc for rationale.
        var send = await mediator.Send(
            new SendActivationEmailCommand(request.Email),
            cancellationToken);

        if (send.IsFailure)
        {
            logger.LogWarning(
                "Auth: Invite for {UserId} / {Email} was provisioned but activation email delivery failed ({Outcome}). Admin should use Resend Invite.",
                userId,
                request.Email,
                send.Outcome);

            return Result<InviteUserResult>.Fail(
                send.Outcome,
                send.Messages.FirstOrDefault() ?? "Account was provisioned but we couldn't send the invite email. Please use Resend Invite.",
                send.Errors.ToArray());
        }

        return Result<InviteUserResult>.Created(new InviteUserResult(userId, profileId));
    }
}
