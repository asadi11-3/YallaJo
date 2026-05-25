using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Blog.GetCreatorBlogsBySlug;

public sealed class GetCreatorBlogsBySlugQueryHandler(
    ICreatorProfileRepository profileRepository,
    IBlogRepository blogRepository)
    : IQueryHandler<GetCreatorBlogsBySlugQuery, PaginatedResult<BlogSummaryDto>>
{
    public async Task<Result<PaginatedResult<BlogSummaryDto>>> Handle(
        GetCreatorBlogsBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository
            .GetBySlugAsync(request.CreatorSlug, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
            return Result<PaginatedResult<BlogSummaryDto>>.Failure(
                CreatorProfileErrors.NotFound, Outcome.NotFound);

        var blogs = await blogRepository
            .GetPublishedByCreatorProfileIdAsync(
                profile.Id,
                request.Page,
                request.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        var dtos = blogs.Items
            .Select(b => new BlogSummaryDto(
                b.Id,
                b.Slug,
                b.Title,
                b.Summary,
                b.PublishedAt,
                b.ViewCount,
                b.ReadTimeMinutes,
                b.PlaceId,
                b.LanguageId.ToString(),   // LanguageCode approximation
                b.IsFeatured))
            .ToList();

        return Result<PaginatedResult<BlogSummaryDto>>.Success(
            new PaginatedResult<BlogSummaryDto>(dtos, blogs.TotalCount, request.Page, request.PageSize));
    }
}
