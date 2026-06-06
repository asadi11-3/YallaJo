using System.Linq;
using System.Reflection;
using ContentBlogs.Application.Commands.Creator.AdminUpdate;
using ContentBlogs.Domain.Entities.Creators;
using FluentAssertions;
using Xunit;

namespace ContentBlogs.Tests.Unit.Domain;

/// <summary>
/// Guards that the CreatorProfile cover-image concept has been fully removed from
/// the data model (domain entity, admin command). Reflection-based so it does not
/// depend on the entity factory and fails loudly if cover image is ever reintroduced.
/// <para>
/// Note: this is intentionally about the CREATOR profile cover only — blog-article
/// and guide cover images are separate features and are NOT covered here.
/// </para>
/// </summary>
public sealed class CreatorProfileCoverImageRemovedTests
{
    [Fact]
    public void CreatorProfile_has_no_CoverImageUrl_property()
    {
        typeof(CreatorProfile)
            .GetProperty("CoverImageUrl", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Should().BeNull("the creator cover image property must be removed from the domain model");
    }

    [Fact]
    public void CreatorProfile_has_no_UpdateCoverImage_method()
    {
        typeof(CreatorProfile)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(m => m.Name == "UpdateCoverImage")
            .Should().BeFalse("the creator cover image domain method must be removed");
    }

    [Fact]
    public void AdminUpdateCreatorProfileCommand_has_no_CoverImageUrl_parameter()
    {
        typeof(AdminUpdateCreatorProfileCommand)
            .GetProperties()
            .Select(p => p.Name)
            .Should().NotContain("CoverImageUrl",
                "the admin update command must no longer accept a creator cover image URL");
    }

    [Fact]
    public void CreatorProfile_still_keeps_AvatarUrl()
    {
        // Sanity guard: removal must NOT touch the avatar concept.
        typeof(CreatorProfile)
            .GetProperty("AvatarUrl", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("avatar support must remain after cover removal");
    }
}
