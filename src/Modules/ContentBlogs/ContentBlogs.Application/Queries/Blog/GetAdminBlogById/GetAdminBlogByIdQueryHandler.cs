using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Queries.Blog.Common;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;

namespace ContentBlogs.Application.Queries.Blog.GetAdminBlogById;

public sealed class GetAdminBlogByIdQueryHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<GetAdminBlogByIdQueryHandler> logger)
    : IQueryHandler<GetAdminBlogByIdQuery, AdminBlogDetailDto>
{
    private const string DefaultLanguageMarker = "default";

    public async Task<Result<AdminBlogDetailDto>> Handle(
        GetAdminBlogByIdQuery request,
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

            if (blog is null)
            {
                logger.LogDebug(
                    "GetAdminBlogById: blog {BlogId} not found (missing or soft-deleted).",
                    request.BlogId);
                return Result<AdminBlogDetailDto>.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);
            }

            // Author-hierarchy guard runs BEFORE the DTO build so we never
            // return edit data (including RowVersion) for content the actor
            // cannot manage.  Returning forbidden here uses the same error
            // shape as the mutation handlers so the Admin UI can switch on
            // a single code (Blog.AuthorHierarchyForbidden).
            var hierarchy = await authorHierarchyGuard
                .EnsureCanManageBlogOwnedByAsync(blog.AuthorId, cancellationToken)
                .ConfigureAwait(false);
            if (!hierarchy.IsSuccess)
            {
                var firstError = hierarchy.Errors.Count > 0
                    ? hierarchy.Errors[0]
                    : new Error(
                        "Blog.AuthorHierarchyForbidden", "You cannot manage content created by a user at the same or higher privilege level.");
                return Result<AdminBlogDetailDto>.Failure(firstError, hierarchy.Outcome);
            }

            var resolvedLanguage = await AcceptLanguageResolver.ResolveAsync(
                request.AcceptLanguage, activeLanguageProvider, cancellationToken)
                .ConfigureAwait(false);

            var translation = resolvedLanguage is null
                ? null
                : blog.BlogTranslations.FirstOrDefault(t => t.LanguageId == resolvedLanguage.Id);

            var languageCode = translation is not null && resolvedLanguage is not null
                ? resolvedLanguage.Code
                : DefaultLanguageMarker;

            // Mirror the public detail query's tour projection so the editor can
            // render linked-tour chips (and drop its raw-GUID inputs). Additive
            // fields only — existing Admin consumers ignore them.
            var linkedTours = blog.BlogTours
                .OrderBy(bt => bt.SortOrder)
                .ThenBy(bt => bt.TourId)
                .Select(bt => new BlogTourSummaryDto(bt.TourId, bt.SortOrder))
                .ToList()
                .AsReadOnly();

            var dto = new AdminBlogDetailDto(
                Id:               blog.Id,
                Slug:             blog.Slug,
                Title:            translation?.Title   ?? blog.Title,
                Content:          translation?.Content ?? blog.Content,
                Summary:          translation?.Summary ?? blog.Summary,
                Status:           blog.Status.ToString(),
                PublishedAt:      blog.PublishedAt,
                ViewCount:        blog.ViewCount,
                ReadTimeMinutes:  blog.ReadTimeMinutes,
                MetaTitle:        blog.MetaTitle,
                MetaDescription:  blog.MetaDescription,
                PlaceId:          blog.PlaceId,
                LanguageCode:     languageCode,
                RowVersion:       blog.RowVersion,
                TourCount:        linkedTours.Count,
                LinkedTours:      linkedTours,
                IsFeatured:       blog.IsFeatured);

            return Result<AdminBlogDetailDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<AdminBlogDetailDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
