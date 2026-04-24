using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using FluentAssertions;

namespace Accounts.Tests.Unit;

/// <summary>
/// Phase 3D — domain-level tests for
/// <see cref="Profile.ResetForReassignment"/>. Verifies the placeholder
/// behavior approved for admin account reassignment:
///   • FirstName="Pending", LastName="Activation" (required fields stay valid).
///   • DisplayName derived from new-email local-part (null when blank).
///   • All optional PII fields cleared to null.
///   • UserId unchanged, row not soft-deleted, UpdatedAt bumped.
/// </summary>
public sealed class ProfileReassignmentTests
{
    private static Profile FullyPopulatedProfile(Guid? userId = null)
    {
        var profile = Profile.Create(userId ?? Guid.NewGuid(), "Alice", "Anderson");
        profile.SetDisplayName("alice-display");
        profile.SetAvatarUrl("https://cdn.example.com/avatars/alice.png");
        profile.UpdateProfile(
            firstName:   "Alice",
            lastName:    "Anderson",
            dateOfBirth: new DateOnly(1990, 1, 2),
            gender:      Gender.Female,
            country:     "USA",
            city:        "Seattle",
            addressLine: "123 Main St");
        return profile;
    }

    [Fact]
    public void ResetForReassignment_ShouldSetFirstNameToPending_AndLastNameToActivation()
    {
        var profile = FullyPopulatedProfile();

        profile.ResetForReassignment("new");

        profile.FirstName.Should().Be("Pending");
        profile.LastName.Should().Be("Activation");
    }

    [Fact]
    public void ResetForReassignment_ShouldClearOptionalPiiFields()
    {
        var profile = FullyPopulatedProfile();

        profile.ResetForReassignment("new");

        profile.AvatarUrl.Should().BeNull();
        profile.DateOfBirth.Should().BeNull();
        profile.Gender.Should().BeNull();
        profile.Country.Should().BeNull();
        profile.City.Should().BeNull();
        profile.AddressLine.Should().BeNull();
    }

    [Fact]
    public void ResetForReassignment_ShouldSetDisplayName_FromEmailLocalPart()
    {
        var profile = FullyPopulatedProfile();

        profile.ResetForReassignment("  bob  ");

        profile.DisplayName.Should().Be("bob",
            "the service trims the local-part before passing it; the domain also trims defensively");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResetForReassignment_WithNullOrWhitespaceLocalPart_ShouldNullDisplayName(string? localPart)
    {
        var profile = FullyPopulatedProfile();

        profile.ResetForReassignment(localPart);

        profile.DisplayName.Should().BeNull();
    }

    [Fact]
    public void ResetForReassignment_ShouldKeepUserId_Unchanged()
    {
        var userId = Guid.NewGuid();
        var profile = FullyPopulatedProfile(userId);
        var profileId = profile.Id;

        profile.ResetForReassignment("new");

        profile.UserId.Should().Be(userId,
            "the logical FK to Security.User is system-required and must not be touched by reassignment");
        profile.Id.Should().Be(profileId, "the Profile aggregate identity must not change");
    }

    [Fact]
    public void ResetForReassignment_ShouldMarkUpdated_AndNotSoftDelete()
    {
        var profile = FullyPopulatedProfile();
        profile.IsDeleted.Should().BeFalse();
        var before = profile.UpdatedAt;

        profile.ResetForReassignment("new");

        profile.UpdatedAt.Should().NotBeNull();
        profile.UpdatedAt.Should().NotBe(before);
        profile.IsDeleted.Should().BeFalse("Phase 3D preserves profile existence; only content is scrubbed");
    }
}
