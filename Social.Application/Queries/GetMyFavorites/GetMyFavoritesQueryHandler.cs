using MediatR;
using Social.Application.Queries.Dtos;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetMyFavorites;

internal sealed class GetMyFavoritesQueryHandler(
    IFavoriteRepository favoriteRepository)
    : IRequestHandler<GetMyFavoritesQuery, Result<FavoritePageDto>>
{
    public async Task<Result<FavoritePageDto>> Handle(GetMyFavoritesQuery request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var (items, nextCursor) = await favoriteRepository.GetByUserPageAsync(
            request.UserId, request.AfterCursor, pageSize, ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result.Success(new FavoritePageDto(dtos, nextCursor));
    }

    internal static FavoriteDto MapToDto(Favorite f) => new(
        f.Id,
        f.UserId,
        f.EntityType.ToString(),
        f.EntityId,
        f.AddedAt);
}
