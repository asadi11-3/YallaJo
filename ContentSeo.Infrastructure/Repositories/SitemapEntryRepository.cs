using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISitemapEntryRepository"/>.
/// Compile-only stub for Wave-4 pre-work — streaming-export / batch-touch
/// overrides will be added during TASK 3 implementation.
/// </summary>
internal sealed class SitemapEntryRepository(ContentSeoDbContext context)
    : EfRepository<SitemapEntry, Guid>(context), ISitemapEntryRepository;
