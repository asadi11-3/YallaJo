using Booking.Contracts.Authorization;
using ContentBlogs.Contracts.Authorization;
using ContentCore.Application.Authorization;
using ContentCore.Domain.Enums;
using ContentPlaces.Contracts.Places;
using ContentTours.Contracts.Authorization;
using FluentAssertions;
using NSubstitute;
using Social.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentCore.Tests.Unit;

/// <summary>
/// CCD-5 security fix verification: proves the ContentCore ownership fan-out routes
/// <see cref="EntityType.Blog"/> to <see cref="IBlogOwnershipService"/> and surfaces
/// its resolution (Blog AuthorId). This is the wiring that makes the attachment-upload
/// ownership guard Blog-aware for both upload endpoints.
/// </summary>
public sealed class EntityOwnershipResolverBlogTests
{
    private static (EntityOwnershipResolver Resolver, IBlogOwnershipService Blogs) Build()
    {
        var blogs = Substitute.For<IBlogOwnershipService>();
        var resolver = new EntityOwnershipResolver(
            Substitute.For<IPlaceOwnershipService>(),
            Substitute.For<ITourOwnershipService>(),
            blogs,
            Substitute.For<IReviewOwnershipService>(),
            Substitute.For<ITourGuideOwnershipService>());
        return (resolver, blogs);
    }

    [Fact]
    public async Task Resolve_Blog_RoutesToBlogOwnershipService_AndSurfacesOwner()
    {
        var (resolver, blogs) = Build();
        var blogId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        blogs.GetBlogOwnershipAsync(blogId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(IsSupported: true, Exists: true, IsDeleted: false, OwnerUserId: authorId));

        var resolution = await resolver.ResolveAsync(EntityType.Blog, blogId, CancellationToken.None);

        resolution.IsSupported.Should().BeTrue("Blog ownership is supported by the resolver");
        resolution.Exists.Should().BeTrue();
        resolution.OwnerUserId.Should().Be(authorId, "the Blog owner is its author");
        await blogs.Received(1).GetBlogOwnershipAsync(blogId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_Blog_PropagatesNotFound()
    {
        var (resolver, blogs) = Build();
        var blogId = Guid.NewGuid();

        blogs.GetBlogOwnershipAsync(blogId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(IsSupported: true, Exists: false, IsDeleted: false, OwnerUserId: null));

        var resolution = await resolver.ResolveAsync(EntityType.Blog, blogId, CancellationToken.None);

        resolution.IsSupported.Should().BeTrue();
        resolution.Exists.Should().BeFalse();
        resolution.OwnerUserId.Should().BeNull();
    }
}
