using MediatR;
using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetFlaggedReviews;

public sealed record GetFlaggedReviewsQuery(
    Guid? AfterCursor,
    int PageSize = 20
) : IRequest<Result<ReviewPageDto>>;
