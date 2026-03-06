using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentCore.Infrastructure.Repositories;

internal sealed class LanguageRepository(ContentCoreDbContext context)
    : EfRepository<Language, Guid>(context), ILanguageRepository
{
    private readonly ContentCoreDbContext _context = context;

    public async Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.Languages
            .FirstOrDefaultAsync(l => l.Code == code, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        return await _context.Languages
            .AnyAsync(l => l.Code == code, ct);
    }
}
