using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class EditorialPinRepository(AnalyticsDbContext context) : EfRepository<EditorialPin, Guid>(context), IEditorialPinRepository
{
    public async Task<IReadOnlyList<EditorialPin>> GetActiveByContextAsync(SuggestionContext contextValue, CancellationToken ct = default)
        => await context.EditorialPins
            .Where(x => x.IsActive && x.Context == contextValue && (!x.ExpiresAt.HasValue || x.ExpiresAt > DateTime.UtcNow))
            .OrderBy(x => x.Position)
            .ToListAsync(ct);
}
