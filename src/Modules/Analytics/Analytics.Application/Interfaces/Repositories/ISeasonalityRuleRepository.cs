using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface ISeasonalityRuleRepository : IRepository<SeasonalityRule, Guid>
{
    Task<IReadOnlyList<SeasonalityRule>> GetActiveByPlaceIdAsync(Guid placeId, CancellationToken ct = default);
    Task<IReadOnlyList<SeasonalityRule>> GetAllActiveAsync(CancellationToken ct = default);
}
