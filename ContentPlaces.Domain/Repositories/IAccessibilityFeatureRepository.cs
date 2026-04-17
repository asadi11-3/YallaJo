using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

public interface IAccessibilityFeatureRepository : IReadRepository<AccessibilityFeature, Guid>, IWriteRepository<AccessibilityFeature, Guid>
{
}
