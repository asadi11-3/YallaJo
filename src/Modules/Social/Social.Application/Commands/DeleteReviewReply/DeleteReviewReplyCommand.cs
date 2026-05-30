using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.DeleteReviewReply;

public sealed record DeleteReviewReplyCommand(
    Guid ReviewId,
    Guid ReplyId,
    Guid CallerUserId,
    bool IsAdmin
) : IRequest<Result>;
