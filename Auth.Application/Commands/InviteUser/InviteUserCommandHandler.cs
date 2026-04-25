using Auth.Application.Commands.ProvisionAccount;
using Auth.Application.Commands.SendActivationEmail;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.InviteUser;

public sealed class InviteUserCommandHandler(
    IMediator mediator,
    ILogger<InviteUserCommandHandler> logger)
    : ICommandHandler<InviteUserCommand, InviteUserResult>
{
    public async Task<Result<InviteUserResult>> Handle(
        InviteUserCommand request,
        CancellationToken cancellationToken)
    {
        
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
