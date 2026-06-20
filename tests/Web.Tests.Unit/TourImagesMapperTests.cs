using FluentAssertions;
using YallaJo.Web.Areas.Provider.Models.TourImages;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

public sealed class TourImagesMapperTests
{
    // Mirrors ApiAssetUrlResolver behaviour: prefix relative paths with the API origin,
    // leave absolute URLs and /assets/ Web-host paths untouched.
    private sealed class StubAssetResolver : IApiAssetUrlResolver
    {
        private const string ApiBase = "https://api.test";

        public string? Resolve(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
                return url;
            if (url.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)) return url;
            return ApiBase + (url.StartsWith('/') ? url : "/" + url);
        }
    }

    private static AttachmentItemResponse Item(string url, int sortOrder = 0) => new()
    {
        Id = Guid.NewGuid(),
        EntityType = "Tour",
        EntityId = Guid.NewGuid(),
        Type = "Image",
        Url = url,
        OriginalFileName = "download.png",
        FileSize = 2048,
        SortOrder = sortOrder,
        UploadedAt = DateTime.UtcNow,
    };

    [Fact]
    public void ToVm_ResolvesRelativeUploadUrl_ToApiOrigin()
    {
        var tourId = Guid.NewGuid();
        var images = new List<AttachmentItemResponse> { Item("/uploads/tours/abc/download.png") };

        var vm = TourImagesMapper.ToVm(tourId, images, new StubAssetResolver());

        vm.Images.Should().HaveCount(1);
        vm.Images[0].Url.Should().Be("https://api.test/uploads/tours/abc/download.png");
    }

    [Fact]
    public void ToVm_LeavesAbsoluteUrl_Untouched()
    {
        var images = new List<AttachmentItemResponse> { Item("https://cdn.example.com/x.png") };

        var vm = TourImagesMapper.ToVm(Guid.NewGuid(), images, new StubAssetResolver());

        vm.Images[0].Url.Should().Be("https://cdn.example.com/x.png");
    }

    [Fact]
    public void ToVm_NeverProducesWebHostRelativeUploadPath()
    {
        var images = new List<AttachmentItemResponse> { Item("/uploads/tours/abc/download.png") };

        var vm = TourImagesMapper.ToVm(Guid.NewGuid(), images, new StubAssetResolver());

        // Bug regression: a bare "/uploads/..." would be requested against the Web host and 404.
        vm.Images[0].Url.Should().NotStartWith("/uploads");
        vm.Images[0].Url.Should().StartWith("https://");
    }

    [Fact]
    public void ToVm_OrdersBySortOrderThenUploadedAt()
    {
        var images = new List<AttachmentItemResponse>
        {
            Item("/uploads/b.png", sortOrder: 2),
            Item("/uploads/a.png", sortOrder: 1),
        };

        var vm = TourImagesMapper.ToVm(Guid.NewGuid(), images, new StubAssetResolver());

        vm.Images[0].Url.Should().Be("https://api.test/uploads/a.png");
        vm.Images[1].Url.Should().Be("https://api.test/uploads/b.png");
    }
}
