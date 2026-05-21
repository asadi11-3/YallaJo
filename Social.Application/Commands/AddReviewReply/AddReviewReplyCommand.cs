using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AddReviewReply;

public sealed record AddReviewReplyCommand(
    Guid ReviewId,
    Guid ProviderUserId,
    string Content
) : IRequest<Result<Guid>>;
