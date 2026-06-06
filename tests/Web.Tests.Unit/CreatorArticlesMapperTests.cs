using FluentAssertions;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Areas.Creator.Models.Articles;

namespace Web.Tests.Unit;

/// <summary>
/// Pure-mapper coverage for the CCD-4 articles flow: status-filter normalization,
/// list mapping (no fake status), admin-get → editor (RowVersion + Status), and
/// editor → create/update bodies.
/// </summary>
public sealed class CreatorArticlesMapperTests
{
    // ── Status filter mapping ──────────────────────────────────────────────────

    [Theory]
    [InlineData("draft", "Draft")]
    [InlineData("PENDINGREVIEW", "PendingReview")]
    [InlineData("Published", "Published")]
    [InlineData("rejected", "Rejected")]
    public void NormalizeStatusFilter_maps_known_names_case_insensitively(string input, string expected)
        => CreatorArticlesMapper.NormalizeStatusFilter(input).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bogus")]
    [InlineData("Removed")] // not an offered creator filter option
    public void NormalizeStatusFilter_returns_null_for_blank_or_unknown(string? input)
        => CreatorArticlesMapper.NormalizeStatusFilter(input).Should().BeNull();

    // ── List mapping ───────────────────────────────────────────────────────────

    [Fact]
    public void ToListVm_maps_rows_with_real_status_and_source_language()
    {
        var page = new PaginatedResponse<BlogSummaryResponse>
        {
            Items =
            [
                new BlogSummaryResponse
                {
                    Id = Guid.NewGuid(), Slug = "a", Title = "A",
                    PublishedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    ViewCount = 10, ReadTimeMinutes = 4,
                    Status = "Draft", SourceLanguageCode = "en",
                },
            ],
            PageNumber = 2, PageSize = 20, TotalCount = 25, TotalPages = 2,
            HasPreviousPage = true, HasNextPage = false,
        };

        var vm = CreatorArticlesMapper.ToListVm(page, "Draft");

        vm.HasArticles.Should().BeTrue();
        vm.Articles.Should().ContainSingle();
        vm.Articles[0].Title.Should().Be("A");
        vm.Articles[0].ViewCount.Should().Be(10);
        vm.StatusFilter.Should().Be("Draft");
        vm.Pager.PageNumber.Should().Be(2);
        vm.Pager.HasPreviousPage.Should().BeTrue();
        vm.Pager.HasNextPage.Should().BeFalse();

        // Gap 1: the list row now carries the real status + source language (no fakery —
        // these come straight from the backend my-blogs summary).
        vm.Articles[0].Status.Should().Be("Draft");
        vm.Articles[0].SourceLanguageCode.Should().Be("en");
    }

    [Fact]
    public void ToListVm_empty_page_has_no_articles()
    {
        var page = new PaginatedResponse<BlogSummaryResponse> { Items = [], PageNumber = 1, PageSize = 20 };
        CreatorArticlesMapper.ToListVm(page, null).HasArticles.Should().BeFalse();
    }

    // ── Editor mapping (admin-get is the RowVersion + Status source) ───────────

    [Fact]
    public void ToEditorVm_carries_RowVersion_and_Status_from_admin_get()
    {
        var admin = new AdminBlogDetailResponse
        {
            Id = Guid.NewGuid(), Slug = "my-post", Title = "My Post", Content = "body",
            Summary = "sum", Status = "Draft", LanguageCode = "en",
            MetaTitle = "mt", MetaDescription = "md", RowVersion = "AAAAAAAAB9E=",
        };

        var vm = CreatorArticlesMapper.ToEditorVm(admin);

        vm.Id.Should().Be(admin.Id);
        vm.RowVersion.Should().Be("AAAAAAAAB9E=", "RowVersion must come from admin-get for safe updates");
        vm.Status.Should().Be("Draft");
        vm.IsNew.Should().BeFalse();
        vm.IsEditable.Should().BeTrue();
        vm.CanSubmit.Should().BeTrue();
        vm.Content.Should().Be("body");
        vm.SourceLanguageCode.Should().Be("en");
    }

    [Theory]
    [InlineData("Draft", true, true)]
    [InlineData("Rejected", true, true)]
    [InlineData("PendingReview", false, false)]
    [InlineData("Published", false, false)]
    public void Editor_editability_and_submit_depend_on_status(string status, bool editable, bool canSubmit)
    {
        var vm = CreatorArticlesMapper.ToEditorVm(new AdminBlogDetailResponse
        {
            Id = Guid.NewGuid(), Title = "T", Content = "c", Status = status, RowVersion = "Ug==",
        });

        vm.IsEditable.Should().Be(editable);
        vm.CanSubmit.Should().Be(canSubmit);
    }

    // ── Editor → bodies ────────────────────────────────────────────────────────

    [Fact]
    public void ToCreateBody_trims_and_defaults_language()
    {
        var form = new ArticleEditorVm
        {
            Title = "  Title  ", Content = "content", SourceLanguageCode = "  ",
            Slug = "  ", Summary = "  s  ", MetaTitle = "  ", MetaDescription = "  d  ",
        };

        var body = CreatorArticlesMapper.ToCreateBody(form);

        body.Title.Should().Be("Title");
        body.SourceLanguageCode.Should().Be("en", "blank language defaults to en");
        body.Slug.Should().BeNull();
        body.Summary.Should().Be("s");
        body.MetaTitle.Should().BeNull();
        body.MetaDescription.Should().Be("d");
    }

    [Fact]
    public void ToUpdateBody_carries_RowVersion_and_falls_back_slug_to_title()
    {
        var form = new ArticleEditorVm
        {
            Id = Guid.NewGuid(), RowVersion = "Ug==", Title = "Hello World",
            Content = "c", Slug = null,
        };

        var body = CreatorArticlesMapper.ToUpdateBody(form);

        body.RowVersion.Should().Be("Ug==", "update must echo the RowVersion from admin-get");
        body.Slug.Should().Be("Hello World", "blank slug falls back to the title");
        body.Title.Should().Be("Hello World");
    }
}
