using Auth.Application.Commands.SendActivationEmail;
using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResendInvite;

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

        if (result.Outcome == Outcome.Conflict)
        {
            return Result<ResendInviteResult>.Fail(
                result.Outcome,
                result.Messages.FirstOrDefault() ?? string.Empty,
                result.Errors.ToArray());
        }

        return Result<ResendInviteResult>.Success(new ResendInviteResult(GenericMessage));
    }
}
