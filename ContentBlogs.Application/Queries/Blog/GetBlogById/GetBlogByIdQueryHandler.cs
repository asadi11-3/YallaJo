using ContentBlogs.Application.Queries.Blog.Common;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;

namespace ContentBlogs.Application.Queries.Blog.GetBlogById;

public sealed class GetBlogByIdQueryHandler(
    IBlogRepository blogRepository,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<GetBlogByIdQueryHandler> logger)
    : IQueryHandler<GetBlogByIdQuery, BlogDetailDto>
{
    public async Task<Result<BlogDetailDto>> Handle(
        GetBlogByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var blog = await blogRepository.GetAsync(
                filter:       b => b.Id == request.BlogId,
                include:      q => q
                    .Include(b => b.BlogTranslations)
                    .Include(b => b.BlogTours),
                asNoTracking: true,
                ct:           cancellationToken)
                .ConfigureAwait(false);

            if (blog is null || blog.Status != BlogStatus.Published)
            {
                logger.LogDebug(
                    "GetBlogById: blog {BlogId} not visible to anonymous caller.", request.BlogId);
                return Result<BlogDetailDto>.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
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

internal static class BlogDetailMapper
{
    private const string DefaultLanguageMarker = "default";

    public static BlogDetailDto Map(BlogEntity blog, ActiveLanguage? resolvedLanguage)
    {
        var translation = resolvedLanguage is null
            ? null
            : blog.BlogTranslations.FirstOrDefault(t => t.LanguageId == resolvedLanguage.Id);

        // If the requested translation exists, surface its language code.
        // Otherwise we are returning the source-language base fields → "default".
        var languageCode = translation is not null && resolvedLanguage is not null
            ? resolvedLanguage.Code
            : DefaultLanguageMarker;

        var linkedTours = blog.BlogTours
            .OrderBy(bt => bt.SortOrder)
            .ThenBy(bt => bt.TourId)
            .Select(bt => new BlogTourSummaryDto(bt.TourId, bt.SortOrder))
            .ToList()
            .AsReadOnly();

        return new BlogDetailDto(
            Id:               blog.Id,
            Slug:             blog.Slug,
            Title:            translation?.Title   ?? blog.Title,
            Content:          translation?.Content ?? blog.Content,
            Summary:          translation?.Summary ?? blog.Summary,
            PublishedAt:      blog.PublishedAt,
            ViewCount:        blog.ViewCount,
            ReadTimeMinutes:  blog.ReadTimeMinutes,
            MetaTitle:        blog.MetaTitle,
            MetaDescription:  blog.MetaDescription,
            PlaceId:          blog.PlaceId,
            LanguageCode:     languageCode,
            TourCount:        linkedTours.Count,
            LinkedTours:      linkedTours);
    }
}
