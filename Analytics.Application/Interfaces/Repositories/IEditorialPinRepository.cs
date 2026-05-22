using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IEditorialPinRepository : IRepository<EditorialPin, Guid>
{
    Task<IReadOnlyList<EditorialPin>> GetActiveByContextAsync(SuggestionContext context, CancellationToken ct = default);
}
