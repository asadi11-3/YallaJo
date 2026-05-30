using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ContentBlogs.Infrastructure.Services;

public sealed class BlogViewCounter(ContentBlogsDbContext dbContext) : IBlogViewCounter
{
    private const int SqlServerDuplicateIndexError      = 2601;
    private const int SqlServerDuplicateConstraintError = 2627;

    public async Task<BlogViewCountResult> TryCountAsync(
        Guid blogId,
        BlogViewerKind viewerKind,
        byte[] viewerHash,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await dbContext.Blogs
            .AsNoTracking()
            .Where(b => b.Id == blogId && b.Status == BlogStatus.Published)
            .Select(b => new { b.Slug, b.ViewCount })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return new BlogViewCountResult(Counted: false, ViewCount: null, Slug: null);
        }

        await using var tx = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var marker = BlogView.Create(blogId, viewerHash, viewerKind, DateTime.UtcNow);
        dbContext.BlogViews.Add(marker);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            dbContext.Entry(marker).State = EntityState.Detached;
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new BlogViewCountResult(
                Counted:   false,
                ViewCount: snapshot.ViewCount,
                Slug:      snapshot.Slug);
        }

        var rows = await dbContext.Blogs
            .Where(b => b.Id == blogId && b.Status == BlogStatus.Published)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(b => b.ViewCount, b => b.ViewCount + 1),
                cancellationToken)
            .ConfigureAwait(false);

        if (rows == 0)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new BlogViewCountResult(Counted: false, ViewCount: null, Slug: null);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new BlogViewCountResult(
            Counted:   true,
            ViewCount: snapshot.ViewCount + 1,
            Slug:      snapshot.Slug);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (Exception? cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is SqlException sql &&
                (sql.Number == SqlServerDuplicateIndexError ||
                 sql.Number == SqlServerDuplicateConstraintError))
            {
                return true;
            }
        }

        return false;
    }
}
