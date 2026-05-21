using MediatR;
using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetMyReviews;

public sealed record GetMyReviewsQuery(
    Guid UserId,
    Guid? AfterCursor,
    int PageSize = 20
) : IRequest<Result<ReviewPageDto>>;
