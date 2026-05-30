using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for the <see cref="Tag"/> entity.
/// <see cref="Tag"/> is not an <c>IAggregateRoot</c>, so this class extends
/// <see cref="EfEntityRepository{TEntity,TKey}"/> rather than <see cref="EfRepository{TEntity,TKey}"/>.
/// </summary>
internal sealed class TagRepository(ContentCoreDbContext context)
    : EfEntityRepository<Tag, Guid>(context), ITagRepository;
