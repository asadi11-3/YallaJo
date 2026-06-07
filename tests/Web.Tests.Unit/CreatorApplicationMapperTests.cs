using FluentAssertions;
using YallaJo.Web.Features.Blogs.Models;
using YallaJo.Web.Areas.Creator.Models.Application;
using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace Web.Tests.Unit;

public sealed class CreatorApplicationMapperTests
{
    private static CreatorApplicationMineResponse App(
        string status,
        IReadOnlyList<string>? portfolio = null,
        IReadOnlyList<Guid>? niches = null,
        IReadOnlyList<string>? tags = null) => new()
    {
        Id              = Guid.NewGuid(),
        ApplicantUserId = Guid.NewGuid(),
        Status          = status,
        Bio             = "hi",
        PortfolioUrls   = portfolio ?? [],
        NicheIds        = niches ?? [],
        FreeTags        = tags ?? [],
        CreatedAt       = DateTime.UtcNow,
    };

    [Fact]
    public void ToNicheOptions_FiltersInactive_AndOrdersBySortOrder()
    {
        var niches = new List<CreatorNicheResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "B", SortOrder = 2, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Inactive", SortOrder = 0, IsActive = false },
            new() { Id = Guid.NewGuid(), Name = "A", SortOrder = 1, IsActive = true },
        };

        var options = CreatorApplicationMapper.ToNicheOptions(niches);

        options.Should().HaveCount(2);
        options.Select(o => o.Name).Should().ContainInOrder("A", "B");
    }

    [Fact]
    public void ToFormVm_NullApplication_IsNewAndEditable()
    {
        var vm = CreatorApplicationMapper.ToFormVm(application: null, nicheOptions: []);

        vm.IsNew.Should().BeTrue();
        vm.IsEditable.Should().BeTrue();
        vm.CanSubmit.Should().BeFalse();
        vm.ApplicationId.Should().BeNull();
    }

    [Theory]
    [InlineData("Draft", true, true)]            // editable, submittable
    [InlineData("MoreInfoNeeded", true, false)]  // editable, not submittable
    [InlineData("Pending", false, false)]        // read-only
    [InlineData("Approved", false, false)]
    [InlineData("Rejected", false, false)]
    public void ToFormVm_EditabilityAndSubmit_DependOnStatus(string status, bool editable, bool canSubmit)
    {
        var vm = CreatorApplicationMapper.ToFormVm(App(status), nicheOptions: []);

        vm.IsNew.Should().BeFalse();
        vm.IsEditable.Should().Be(editable);
        vm.CanSubmit.Should().Be(canSubmit);
        vm.Status.Should().Be(status);
    }

    [Fact]
    public void ToFormVm_PrefillsCollectionsAsText()
    {
        var nicheId = Guid.NewGuid();
        var vm = CreatorApplicationMapper.ToFormVm(
            App("Draft", portfolio: ["https://a", "https://b"], niches: [nicheId], tags: ["x", "y"]),
            nicheOptions: []);

        vm.PortfolioUrlsText.Should().Contain("https://a").And.Contain("https://b");
        vm.FreeTagsText.Should().Contain("x").And.Contain("y");
        vm.SelectedNicheIds.Should().ContainSingle().Which.Should().Be(nicheId);
    }

    [Fact]
    public void ToCreateBody_TrimsParsesAndSendsEmptyLanguageRegion()
    {
        var form = new CreatorApplicationFormVm
        {
            Bio                = "  my bio  ",
            PortfolioUrlsText  = "https://a\nhttps://b\n\nhttps://c",
            SampleWorkUrlsText = "https://s1",
            FreeTagsText       = "tag1, tag2 ,tag3",
            SelectedNicheIds   = [Guid.NewGuid(), Guid.NewGuid()],
        };

        var body = CreatorApplicationMapper.ToCreateBody(form);

        body.Bio.Should().Be("my bio");
        body.PortfolioUrls.Should().BeEquivalentTo(["https://a", "https://b", "https://c"]);
        body.SampleWorkUrls.Should().ContainSingle().Which.Should().Be("https://s1");
        body.FreeTags.Should().BeEquivalentTo(["tag1", "tag2", "tag3"]);
        body.NicheIds.Should().HaveCount(2);
        // CCD-2 scope: language/region pickers deferred → always empty.
        body.LanguageIds.Should().BeEmpty();
        body.PreferredRegionIds.Should().BeEmpty();
        body.SocialHandles.Should().BeEmpty();
    }

    [Fact]
    public void ToCreateBody_EnforcesServerLimits()
    {
        var manyUrls = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"https://u{i}"));
        var manyTags = string.Join(",", Enumerable.Range(1, 40).Select(i => $"t{i}"));
        var manyNiches = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();

        var form = new CreatorApplicationFormVm
        {
            PortfolioUrlsText = manyUrls,
            FreeTagsText      = manyTags,
            SelectedNicheIds  = manyNiches,
        };

        var body = CreatorApplicationMapper.ToCreateBody(form);

        body.PortfolioUrls.Should().HaveCount(10, "max 10 portfolio URLs");
        body.FreeTags.Should().HaveCount(20, "max 20 free tags");
        body.NicheIds.Should().HaveCount(5, "max 5 niches");
    }

    [Fact]
    public void ToCreateBody_TruncatesLongFreeTags()
    {
        var longTag = new string('x', 80);
        var form = new CreatorApplicationFormVm { FreeTagsText = longTag };

        var body = CreatorApplicationMapper.ToCreateBody(form);

        body.FreeTags.Should().ContainSingle().Which.Length.Should().Be(50, "free tags are capped at 50 chars");
    }

    [Fact]
    public void ToUpdateBody_HasSameShapeAsCreate()
    {
        var form = new CreatorApplicationFormVm { Bio = "b", FreeTagsText = "t" };

        var update = CreatorApplicationMapper.ToUpdateBody(form);

        update.Bio.Should().Be("b");
        update.FreeTags.Should().ContainSingle().Which.Should().Be("t");
        update.LanguageIds.Should().BeEmpty();
        update.PreferredRegionIds.Should().BeEmpty();
    }
}
