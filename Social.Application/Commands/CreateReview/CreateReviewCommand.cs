using MediatR;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.CreateReview;

public sealed record CreateReviewCommand(
    Guid UserId,
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate
) : IRequest<Result<Guid>>;
