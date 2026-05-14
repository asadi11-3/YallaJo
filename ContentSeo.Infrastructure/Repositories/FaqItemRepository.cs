using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFaqItemRepository"/>.
/// Compile-only stub for Wave-4 pre-work — list-by-entity / reorder overrides
/// will be added during TASK 3 implementation.
/// </summary>
internal sealed class FaqItemRepository(ContentSeoDbContext context)
    : EfRepository<FaqItem, Guid>(context), IFaqItemRepository
{
    public async Task<IReadOnlyList<FaqItem>> GetByEntityAsync(SeoEntityType seoEntityType, Guid entityId, CancellationToken ct = default)
        => await GetAllAsync(
            filter: f => f.EntityType == seoEntityType && f.EntityId == entityId && !f.IsDeleted,
            orderBy: q => q.OrderBy(f => f.SortOrder),
            asNoTracking: false,
            ct: ct);

    public async Task<IReadOnlyList<FaqItem>> GetByEntityWithTranslationsAsync(SeoEntityType seoEntityType, Guid entityId, CancellationToken ct = default)
        => await GetAllAsync(
            filter: f => f.EntityType == seoEntityType && f.EntityId == entityId && !f.IsDeleted,
            include: q => q.Include(f => f.FaqItemTranslations),
            orderBy: q => q.OrderBy(f => f.SortOrder),
            asNoTracking: true,
            ct: ct);
  
}
