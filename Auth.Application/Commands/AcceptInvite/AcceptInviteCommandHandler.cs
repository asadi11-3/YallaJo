using Auth.Application.Commands.ActivateAccount;
using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AcceptInvite;

/// <summary>
/// Phase 2B — legacy accept-invite entry point. Retained as a thin façade
/// over <see cref="ActivateAccountCommand"/>. The HTTP endpoint contract
/// (<c>POST /invitations/accept</c>) and response DTO
/// (<see cref="AcceptInviteResult"/>) are unchanged — only the underlying
/// command and the lifecycle-transition discipline were split out.
/// </summary>
public sealed class AcceptInviteCommandHandler(IMediator mediator)
    : ICommandHandler<AcceptInviteCommand, AcceptInviteResult>
{
    public async Task<Result<AcceptInviteResult>> Handle(
        AcceptInviteCommand request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ActivateAccountCommand(
                request.Email,
                request.Token,
                request.Password,
                request.ConfirmPassword),
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<AcceptInviteResult>.Fail(
                result.Outcome,
                result.Messages.FirstOrDefault() ?? string.Empty,
                result.Errors.ToArray());
        }

        return Result<AcceptInviteResult>.Success(new AcceptInviteResult(result.Value.UserId));
    }
}
