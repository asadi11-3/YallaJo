using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.DeleteReview;

public sealed record DeleteReviewCommand(
    Guid ReviewId,
    Guid CallerUserId,
    bool IsAdmin,
    byte[] RowVersion
) : IRequest<Result>;
