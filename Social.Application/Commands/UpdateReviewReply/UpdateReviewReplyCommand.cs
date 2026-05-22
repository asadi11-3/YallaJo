using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.UpdateReviewReply;

public sealed record UpdateReviewReplyCommand(
    Guid ReviewId,
    Guid ReplyId,
    Guid CallerUserId,
    string Content
) : IRequest<Result>;
