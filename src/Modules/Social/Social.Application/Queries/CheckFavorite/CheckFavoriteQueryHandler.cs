using MediatR;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.CheckFavorite;

internal sealed class CheckFavoriteQueryHandler(
    IFavoriteRepository favoriteRepository)
    : IRequestHandler<CheckFavoriteQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(CheckFavoriteQuery request, CancellationToken ct)
    {
        var favorite = await favoriteRepository.GetByUserAndEntityAsync(
            request.UserId, request.EntityType, request.EntityId, ct);

        var isFavorited = favorite is not null && !favorite.IsDeleted;
        return Result.Success(isFavorited);
    }
}
