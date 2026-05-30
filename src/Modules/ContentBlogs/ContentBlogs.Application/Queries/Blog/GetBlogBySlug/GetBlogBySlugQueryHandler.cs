using ContentBlogs.Application.Queries.Blog.Common;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Blog.GetBlogById;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Blog.GetBlogBySlug;

public sealed class GetBlogBySlugQueryHandler(
    IBlogRepository blogRepository,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<GetBlogBySlugQueryHandler> logger)
    : IQueryHandler<GetBlogBySlugQuery, BlogDetailDto>
{
    public async Task<Result<BlogDetailDto>> Handle(
        GetBlogBySlugQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalized = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();

            var blog = await blogRepository.GetAsync(
                filter:       b => b.Slug == normalized,
                include:      q => q
                    .Include(b => b.BlogTranslations)
                    .Include(b => b.BlogTours),
                asNoTracking: true,
                ct:           cancellationToken)
                .ConfigureAwait(false);

            if (blog is null || blog.Status != BlogStatus.Published)
            {
                logger.LogDebug(
                    "GetBlogBySlug: slug {Slug} not visible to anonymous caller.", normalized);
                return Result<BlogDetailDto>.Failure(
                    new Error("Blog.NotFound", $"Blog with slug '{normalized}' was not found."),
                    Outcome.NotFound);
            }

            var resolvedLanguage = await AcceptLanguageResolver.ResolveAsync(
                request.AcceptLanguage, activeLanguageProvider, cancellationToken)
                .ConfigureAwait(false);

            return Result<BlogDetailDto>.Success(BlogDetailMapper.Map(blog, resolvedLanguage));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<BlogDetailDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
