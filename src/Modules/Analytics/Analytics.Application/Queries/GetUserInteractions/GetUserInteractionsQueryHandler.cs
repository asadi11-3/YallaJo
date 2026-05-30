using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetUserInteractions;

public sealed class GetUserInteractionsQueryHandler(IUserInteractionRepository repo, ILogger<GetUserInteractionsQueryHandler> logger) : IQueryHandler<GetUserInteractionsQuery, CursorPageDto<UserInteractionDto>>
{
    public async Task<Result<CursorPageDto<UserInteractionDto>>> Handle(GetUserInteractionsQuery request, CancellationToken ct)
    {
        var page = await repo.GetPageAsync(request.UserId, null, null, null, null, null, request.AfterId, request.PageSize, ct);
        logger.LogDebug("Read {Count} user interactions", page.Items.Count);
        return Result.Success(new CursorPageDto<UserInteractionDto>(page.Items.Select(x => new UserInteractionDto(x.Id, x.UserId, x.EntityType.ToString(), x.EntityId, x.InteractionType.ToString(), x.OccurredAt, x.UserAgent)).ToList(), page.NextId));
    }
}
