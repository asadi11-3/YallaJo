using FluentAssertions;
using YallaJo.Web.Areas.Creator.Models.Articles.Images;

namespace Web.Tests.Unit;

/// <summary>
/// Pure-mapper coverage for the CCD-5 article images flow: response→VM ordering,
/// capacity flags, and client-side file validation (type/size/empty).
/// </summary>
public sealed class CreatorArticleImagesMapperTests
{
    private static AttachmentItemResponse Img(Guid id, int sortOrder, string url = "https://cdn/x.jpg") => new()
    {
        Id = id, EntityType = "Blog", EntityId = Guid.NewGuid(), Type = "Image",
        Url = url, SortOrder = sortOrder, UploadedAt = DateTime.UtcNow,
    };

    [Fact]
    public void ToVm_OrdersBySortOrder_AndSetsCapacityFlags()
    {
        var blogId = Guid.NewGuid();
        var a = Img(Guid.NewGuid(), 2);
        var b = Img(Guid.NewGuid(), 0);
        var c = Img(Guid.NewGuid(), 1);

        var vm = ArticleImagesMapper.ToVm(blogId, [a, b, c]);

        vm.BlogId.Should().Be(blogId);
        vm.HasImages.Should().BeTrue();
        vm.Images.Select(i => i.SortOrder).Should().ContainInOrder(0, 1, 2);
        vm.MaxImages.Should().Be(20);
        vm.CanAddMore.Should().BeTrue();
        vm.RemainingSlots.Should().Be(17);
    }

    [Fact]
    public void ToVm_Empty_HasNoImages_AndCanAddMore()
    {
        var vm = ArticleImagesMapper.ToVm(Guid.NewGuid(), []);
        vm.HasImages.Should().BeFalse();
        vm.CanAddMore.Should().BeTrue();
        vm.RemainingSlots.Should().Be(20);
    }

    [Fact]
    public void ToVm_AtCapacity_CannotAddMore()
    {
        var items = Enumerable.Range(0, 20).Select(i => Img(Guid.NewGuid(), i)).ToList();
        var vm = ArticleImagesMapper.ToVm(Guid.NewGuid(), items);

        vm.Images.Should().HaveCount(20);
        vm.CanAddMore.Should().BeFalse();
        vm.RemainingSlots.Should().Be(0);
    }

    [Fact]
    public void ToVm_RowDisplayUrl_PrefersThumbnail()
    {
        var item = new AttachmentItemResponse
        {
            Id = Guid.NewGuid(), EntityType = "Blog", EntityId = Guid.NewGuid(), Type = "Image",
            Url = "https://cdn/full.jpg", ThumbnailUrl = "https://cdn/thumb.jpg",
            SortOrder = 0, UploadedAt = DateTime.UtcNow,
        };
        var vm = ArticleImagesMapper.ToVm(Guid.NewGuid(), [item]);
        vm.Images[0].DisplayUrl.Should().Be("https://cdn/thumb.jpg");
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg", 1024)]
    [InlineData("photo.PNG", "image/png", 5_000_000)]
    [InlineData("anim.gif", "image/gif", 2048)]
    [InlineData("pic.webp", "image/webp", 2048)]
    public void ValidateImage_AcceptsAllowedTypes(string name, string ct, long size)
        => ArticleImagesMapper.ValidateImage(name, ct, size).Should().BeNull();

    [Fact]
    public void ValidateImage_RejectsSvg()
        => ArticleImagesMapper.ValidateImage("x.svg", "image/svg+xml", 100)
            .Should().NotBeNull().And.Contain("unsupported");

    [Fact]
    public void ValidateImage_RejectsTooLarge()
        => ArticleImagesMapper.ValidateImage("big.jpg", "image/jpeg", 11L * 1024 * 1024)
            .Should().NotBeNull().And.Contain("10 MB");

    [Fact]
    public void ValidateImage_RejectsEmpty()
        => ArticleImagesMapper.ValidateImage("empty.jpg", "image/jpeg", 0)
            .Should().NotBeNull().And.Contain("empty");

    [Fact]
    public void ValidateImage_RejectsUnknownExtension()
        => ArticleImagesMapper.ValidateImage("doc.pdf", "application/pdf", 100)
            .Should().NotBeNull().And.Contain("unsupported");
}
