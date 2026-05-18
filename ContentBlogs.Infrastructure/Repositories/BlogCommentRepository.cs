using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentBlogs.Infrastructure.Repositories;

public class BlogCommentRepository(ContentBlogsDbContext context)
    : EfRepository<BlogComment, Guid>(context), IBlogCommentRepository
{
    private readonly ContentBlogsDbContext _context = context;

    public Task<BlogComment?> GetWithParentChainAsync(
        Guid commentId,
        CancellationToken cancellationToken = default) =>
        _context.BlogComments
            .Include(c => c.ParentComment!)
                .ThenInclude(p => p.ParentComment)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

    public Task<BlogComment?> GetWithReactionsAsync(
        Guid commentId,
        CancellationToken cancellationToken = default) =>
        _context.BlogComments
            .Include(c => c.Reactions)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
}
