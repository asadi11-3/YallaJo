using ContentCore.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentCore.Domain.Repositories;

public interface ITagRepository : IReadRepository<Tag, Guid>, IWriteRepository<Tag, Guid>
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid excludeId, CancellationToken ct = default);
}
