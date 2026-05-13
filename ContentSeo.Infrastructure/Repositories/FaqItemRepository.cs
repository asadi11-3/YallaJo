using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFaqItemRepository"/>.
/// Compile-only stub for Wave-4 pre-work — list-by-entity / reorder overrides
/// will be added during TASK 3 implementation.
/// </summary>
internal sealed class FaqItemRepository(ContentSeoDbContext context)
    : EfRepository<FaqItem, Guid>(context), IFaqItemRepository;
