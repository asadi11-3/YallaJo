using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Repository for the <see cref="FaqItem"/> aggregate root.
/// Compile-only stub for Wave-4 pre-work — list-by-entity / reorder query
/// methods will be added during TASK 3 implementation.
/// </summary>
public interface IFaqItemRepository : IRepository<FaqItem>
{
    Task<IReadOnlyList<FaqItem>> GetByEntityWithTranslationsAsync(SeoEntityType seoEntityType,Guid entityId , CancellationToken ct =default);

    Task<IReadOnlyList<FaqItem>> GetByEntityAsync(SeoEntityType seoEntityType, Guid entityId, CancellationToken ct = default);
}
