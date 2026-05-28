using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentBlogs.Infrastructure.Repositories;

public class CreatorApplicationRepository(ContentBlogsDbContext context)
    : EfRepository<CreatorApplication, Guid>(context), ICreatorApplicationRepository
{
    public Task<CreatorApplication?> GetLatestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorApplications
            .Where(a => a.ApplicantUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasActiveApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorApplications
            .AnyAsync(
                a => a.ApplicantUserId == userId &&
                     (a.Status == CreatorApplicationStatus.Draft ||
                      a.Status == CreatorApplicationStatus.Pending ||
                      a.Status == CreatorApplicationStatus.MoreInfoNeeded ||
                      a.Status == CreatorApplicationStatus.Approved),
                cancellationToken);
    }

    public Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorApplications
            .CountAsync(a => a.ApplicantUserId == userId, cancellationToken);
    }
}
