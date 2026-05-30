using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Domain.Repositories;

/// <summary>
/// Repository for the <see cref="FaqItem"/> aggregate root.
/// </summary>
public interface IFaqItemRepository : IRepository<FaqItem>
{
    Task<IReadOnlyList<FaqItem>> GetByEntityWithTranslationsAsync(SeoEntityType seoEntityType, Guid entityId, CancellationToken ct = default);

    Task<IReadOnlyList<FaqItem>> GetByEntityAsync(SeoEntityType seoEntityType, Guid entityId, CancellationToken ct = default);
}
