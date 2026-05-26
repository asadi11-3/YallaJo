using Analytics.Application.Interfaces.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetUserDataExport;

public sealed record UserDataExportDto(
    Guid UserId,
    IReadOnlyList<object> Interactions,
    object? Preferences,
    IReadOnlyList<object> ExcludedEntities,
    DateTime ExportedAt);

public sealed record GetUserDataExportQuery : IQuery<UserDataExportDto>;

internal sealed class GetUserDataExportQueryHandler(
    IUserInteractionRepository interactionRepo,
    IUserPreferenceRepository preferenceRepo,
    ICurrentUser currentUser) : IQueryHandler<GetUserDataExportQuery, UserDataExportDto>
{
    public async Task<Result<UserDataExportDto>> Handle(GetUserDataExportQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var (interactions, _) = await interactionRepo.GetPageAsync(
            userId, null, null, null, null, null, null, 1000, cancellationToken);
        var preference = await preferenceRepo.GetByUserIdAsync(userId, cancellationToken);

        var interactionDtos = interactions
            .Select(i => (object)new { i.Id, EntityType = i.EntityType.ToString(), i.EntityId, InteractionType = i.InteractionType.ToString(), i.OccurredAt })
            .ToList();

        var prefDto = preference is null ? null : (object)new
        {
            preference.BudgetTier,
            preference.IsFamilyTraveler,
            preference.CurrentTripStage
        };

        return Result.Success(new UserDataExportDto(userId, interactionDtos, prefDto, [], DateTime.UtcNow));
    }
}
