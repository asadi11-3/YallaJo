using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetAdminInteractions;

public sealed class GetAdminInteractionsQueryHandler(IUserInteractionRepository repo, ILogger<GetAdminInteractionsQueryHandler> logger) : IQueryHandler<GetAdminInteractionsQuery, CursorPageDto<UserInteractionDto>>
{
    public async Task<Result<CursorPageDto<UserInteractionDto>>> Handle(GetAdminInteractionsQuery request, CancellationToken ct)
    {
        EntityType? et = Enum.TryParse<EntityType>(request.EntityType, true, out var e) ? e : null;
        InteractionType? it = Enum.TryParse<InteractionType>(request.InteractionType, true, out var i) ? i : null;
        var page = await repo.GetPageAsync(request.UserId, et, request.EntityId, it, request.From, request.To, request.AfterId, request.PageSize, ct);
        logger.LogDebug("Read {Count} analytics interactions", page.Items.Count);
        return Result.Success(new CursorPageDto<UserInteractionDto>(page.Items.Select(Map).ToList(), page.NextId));
    }
    private static UserInteractionDto Map(UserInteraction x) => new(x.Id, x.UserId, x.EntityType.ToString(), x.EntityId, x.InteractionType.ToString(), x.OccurredAt, x.UserAgent);
}
