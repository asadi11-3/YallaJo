using MediatR;
using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetMyFavorites;

/// <summary>Returns a cursor-paginated list of the caller's favorites.</summary>
public sealed record GetMyFavoritesQuery(
    Guid UserId,
    Guid? AfterCursor = null,
    int PageSize = 20) : IRequest<Result<FavoritePageDto>>;
