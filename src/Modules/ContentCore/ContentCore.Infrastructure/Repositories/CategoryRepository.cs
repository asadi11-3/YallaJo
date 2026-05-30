using System.Linq.Expressions;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="Category"/> aggregates.
/// Delegates the full read/write surface to <see cref="EfRepository{TEntity,TKey}"/>.
///
/// The two domain-specific include methods isolate EF Core's Include() API to this
/// Infrastructure class, preventing Application handlers from depending on
/// Microsoft.EntityFrameworkCore (ISSUE-001 / B2).
/// </summary>
internal sealed class CategoryRepository(ContentCoreDbContext context)
    : EfRepository<Category, Guid>(context), ICategoryRepository
{
    /// <inheritdoc />
    public Task<Category?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default)
        => context.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
