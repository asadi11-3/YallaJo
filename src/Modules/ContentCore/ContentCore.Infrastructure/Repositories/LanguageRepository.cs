using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="Language"/> aggregates.
/// Delegates the full read/write surface to <see cref="EfRepository{TEntity,TKey}"/>.
/// </summary>
internal sealed class LanguageRepository(ContentCoreDbContext context)
    : EfRepository<Language, Guid>(context), ILanguageRepository;
