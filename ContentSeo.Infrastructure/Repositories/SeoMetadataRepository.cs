using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISeoMetadataRepository"/>.
/// Compile-only stub for Wave-4 pre-work — handler-specific overrides will be
/// added during TASK 3 implementation.
/// </summary>
internal sealed class SeoMetadataRepository(ContentSeoDbContext context)
    : EfRepository<SeoMetadata, Guid>(context), ISeoMetadataRepository;
