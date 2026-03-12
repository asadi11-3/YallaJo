using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class CategoryRepository(ContentCoreDbContext context)
    : EfRepository<Category, Guid>(context), ICategoryRepository
{
    public async Task<List<Category>> GetAllWithTranslationsAsync(CancellationToken ct)
    {
        // ملاحظة: نجيب الكاتيجوري مع الترجمات حتى الـ query handler يبني الشجرة
        return await context.Categories
            .Include(c => c.Translations)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Category>> GetByIdsAsync(
        List<Guid> ids,
        CancellationToken ct,
        bool asNoTracking = false)
    {
        //  منجيب العناصر المطلوبة بس حتى نرتبها مرة وحدة
        var query = context.Categories
            .Where(c => ids.Contains(c.Id));

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(ct);
    }
}