
using Auth.Application.Commands.ActivateAccount;
using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AcceptInvite;

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
