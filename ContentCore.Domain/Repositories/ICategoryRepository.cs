using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

public interface ICategoryRepository : IRepository<Category, Guid>
{
    //  هي للقراءة مع الترجمات وبناء الشجرة
    Task<List<Category>> GetAllWithTranslationsAsync(CancellationToken ct);

    //  نستخدمها بعملية الترتيب batch حسب مجموعة ids
    Task<List<Category>> GetByIdsAsync(
        List<Guid> ids,
        CancellationToken ct,
        bool asNoTracking = false);
}