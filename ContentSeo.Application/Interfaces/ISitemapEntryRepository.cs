using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Repository for the <see cref="SitemapEntry"/> aggregate root.
/// Compile-only stub for Wave-4 pre-work — streaming/sitemap-rendering
/// query methods will be added during TASK 3 implementation.
/// </summary>
public interface ISitemapEntryRepository : IRepository<SitemapEntry>;
