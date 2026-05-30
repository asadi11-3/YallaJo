using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.EditReview;

public sealed record EditReviewCommand(
    Guid ReviewId,
    Guid CallerUserId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate
) : IRequest<Result>;
