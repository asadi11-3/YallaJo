using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class TagRepository(ContentCoreDbContext context)
    : EfEntityRepository<Tag, Guid>(context), ITagRepository
{
    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => await _context.Set<Tag>().AnyAsync(t => t.Slug == slug, ct);

    public async Task<bool> SlugExistsAsync(string slug, Guid excludeId, CancellationToken ct = default)
        => await _context.Set<Tag>().AnyAsync(t => t.Slug == slug && t.Id != excludeId, ct);
}
