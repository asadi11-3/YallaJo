using Accounts.Application.Commands.UpdateAvatar;
using FluentAssertions;

namespace Accounts.Tests.Unit;

public sealed class UpdateAvatarCommandValidatorTests
{
    private static readonly UpdateAvatarCommandValidator Sut = new();

    [Fact]
    public void Validate_ShouldAccept_RootedRelativeAvatarUrl()
    {
        var result = Sut.Validate(new UpdateAvatarCommand("/uploads/avatars/a.png"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldAccept_AbsoluteAvatarUrl()
    {
        var result = Sut.Validate(new UpdateAvatarCommand("https://cdn.example.com/avatars/a.png"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldReject_NonRootedRelativeAvatarUrl()
    {
        var result = Sut.Validate(new UpdateAvatarCommand("uploads/avatars/a.png"));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().Contain("AvatarUrl must be an absolute URL or a rooted relative path.");
    }
}
