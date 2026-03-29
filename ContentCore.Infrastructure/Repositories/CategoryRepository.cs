using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class CategoryRepository(ContentCoreDbContext context)
    : EfRepository<Category, Guid>(context), ICategoryRepository
{
    /// <inheritdoc/>
    public async Task<List<Category>> GetAllWithTranslationsAsync(
        Expression<Func<Category, bool>>? filter = null,
        Func<IQueryable<Category>, IOrderedQueryable<Category>>? orderBy = null,
        CancellationToken ct = default)
    {
        IQueryable<Category> query = context.Categories
            .Include(c => c.Translations)
            .AsNoTracking();

        if (filter is not null)
            query = query.Where(filter);

        if (orderBy is not null)
            return await orderBy(query).ToListAsync(ct);

        return await query.ToListAsync(ct);
    }


    /// <inheritdoc/>
    public async Task<Category?> GetByIdWithTranslationsAsync(Guid id, CancellationToken ct = default)
        => await context.Categories
            .Include(c => c.Translations)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => await context.Categories
            .AnyAsync(c => c.Slug == slug, ct);

    /// <inheritdoc/>
    public async Task<bool> SlugExistsAsync(string slug, Guid excludeId, CancellationToken ct = default)
        => await context.Categories
            .AnyAsync(c => c.Slug == slug && c.Id != excludeId, ct);

    /// <inheritdoc/>
    public async Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken ct = default)
        => await context.Categories
            .AnyAsync(c => c.ParentCategoryId == categoryId, ct);
}
