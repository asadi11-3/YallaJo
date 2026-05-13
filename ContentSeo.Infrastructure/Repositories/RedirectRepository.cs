using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRedirectRepository"/>.
/// Compile-only stub for Wave-4 pre-work — chain-flatten / lookup-by-old-url
/// overrides will be added during TASK 3 implementation.
/// </summary>
internal sealed class RedirectRepository(ContentSeoDbContext context)
    : EfRepository<Redirect, Guid>(context), IRedirectRepository;
