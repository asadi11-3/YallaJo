using FluentAssertions;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Areas.Creator.Models.Preview;

namespace Web.Tests.Unit;

/// <summary>
/// Pure-mapper coverage for the CCD-6 public-profile preview: state factories, Active
/// projection of public-safe fields, published-articles mapping/empty, and a structural
/// guard that the public VM never exposes internal fields.
/// </summary>
public sealed class CreatorPreviewMapperTests
{
    // Gap 4: the public CreatorProfileResponse no longer carries UserId/Status/
    // LinkedProviderId/CreatedAt — the backend public DTO never returns them — so the
    // preview's public-safety guarantee is now enforced at the contract level too.
    private static CreatorProfileResponse PublicProfile() => new()
    {
        Id = Guid.NewGuid(),
        Slug = "jane-creator",
        DisplayName = "Jane Creator",
        Bio = "hello world",
        AvatarUrl = "https://cdn/a.jpg",
        TrustTier = "Trusted",
        ArticleCount = 7,
        TotalViewCount = 1234,
        TotalReactionCount = 56,
        TotalCommentCount = 12,
        FollowerCount = 89,
    };

    private static PaginatedResponse<BlogSummaryResponse> Blogs(params BlogSummaryResponse[] items) => new()
    {
        Items = items.ToList(),
        PageNumber = 1, PageSize = 20, TotalCount = items.Length, TotalPages = items.Length > 0 ? 1 : 0,
        HasPreviousPage = false, HasNextPage = false,
    };

    [Fact]
    public void NoProfile_and_Unavailable_factories_setState()
    {
        PublicProfilePreviewMapper.NoProfile().State.Should().Be(PreviewState.NoProfile);
        PublicProfilePreviewMapper.Unavailable().State.Should().Be(PreviewState.Unavailable);
    }

    [Fact]
    public void Active_maps_public_safe_fields()
    {
        var vm = PublicProfilePreviewMapper.Active(PublicProfile(), Blogs(
            new BlogSummaryResponse
            {
                Id = Guid.NewGuid(), Slug = "post-1", Title = "Post 1", Summary = "s1",
                PublishedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), ViewCount = 10, ReadTimeMinutes = 4,
            }));

        vm.State.Should().Be(PreviewState.Active);
        vm.DisplayName.Should().Be("Jane Creator");
        vm.Bio.Should().Be("hello world");
        vm.AvatarUrl.Should().Be("https://cdn/a.jpg");
        vm.TrustTier.Should().Be("Trusted");
        vm.Slug.Should().Be("jane-creator");
        vm.ArticleCount.Should().Be(7);
        vm.FollowerCount.Should().Be(89);
        vm.TotalViewCount.Should().Be(1234);
        vm.TotalReactionCount.Should().Be(56);
        vm.TotalCommentCount.Should().Be(12);
        vm.HasArticles.Should().BeTrue();
        vm.Articles.Should().ContainSingle();
        vm.Articles[0].Slug.Should().Be("post-1");
        vm.Articles[0].Title.Should().Be("Post 1");
        vm.Articles[0].ViewCount.Should().Be(10);
    }

    [Fact]
    public void Active_withNoBlogs_hasEmptyArticleList()
    {
        var vm = PublicProfilePreviewMapper.Active(PublicProfile(), Blogs());
        vm.HasArticles.Should().BeFalse();
        vm.Articles.Should().BeEmpty();
    }

    [Fact]
    public void Active_withNullBlogs_degradesToEmpty()
    {
        var vm = PublicProfilePreviewMapper.Active(PublicProfile(), blogs: null);
        vm.State.Should().Be(PreviewState.Active);
        vm.HasArticles.Should().BeFalse();
    }

    [Fact]
    public void PublicVm_has_no_internal_fields()
    {
        // Structural public-safety guard: the preview VM must not expose internal fields.
        var props = typeof(PublicProfilePreviewVm).GetProperties().Select(p => p.Name).ToList();
        props.Should().NotContain("UserId");
        props.Should().NotContain("Status");
        props.Should().NotContain("LinkedProviderId");
        props.Should().NotContain("CreatedAt");

        var rowProps = typeof(PreviewArticleRowVm).GetProperties().Select(p => p.Name).ToList();
        rowProps.Should().NotContain("Status");
        rowProps.Should().NotContain("AuthorId");
    }

    [Fact]
    public void PublicCreatorProfileResponse_has_no_internal_fields()
    {
        // Gap 4: the Web projection of the public creator profile must not even declare
        // the internal fields the anonymous endpoint no longer returns.
        var props = typeof(CreatorProfileResponse).GetProperties().Select(p => p.Name).ToHashSet();
        props.Should().NotContain("UserId");
        props.Should().NotContain("Status");
        props.Should().NotContain("LinkedProviderId");
        props.Should().NotContain("CreatedAt");
    }
}
