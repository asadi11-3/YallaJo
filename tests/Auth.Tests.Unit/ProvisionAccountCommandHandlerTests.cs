using Accounts.Contracts.Abstractions;
using Auth.Application.Commands.ProvisionAccount;
using FluentAssertions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2B — verifies that <see cref="ProvisionAccountCommandHandler"/>:
///   • creates the Security identity via the Provisioned-only path,
///   • creates the Accounts profile shell linked to that identity,
///   • does NOT issue activation tokens or send activation emails,
///   • surfaces failures from either sub-step without silently recovering.
/// The handler is deliberately not responsible for the
/// <c>Provisioned → PendingActivation</c> transition — that happens only
/// after <c>SendActivationEmailCommand</c> delivers the email.
/// </summary>
public sealed class ProvisionAccountCommandHandlerTests
{
    private readonly IUserRegistrationService _users    = Substitute.For<IUserRegistrationService>();
    private readonly IProfileCreationService  _profiles = Substitute.For<IProfileCreationService>();

    private ProvisionAccountCommandHandler CreateSut() => new(_users, _profiles);

    private static ProvisionAccountCommand Command(string email = "new.user@example.com") =>
        new(
            Email:          email,
            FirstName:      "Jo",
            LastName:       "Doe",
            DisplayName:    "  Jo D.  ",
            AvatarUrl:      "  https://cdn/a.png  ",
            InitialRoleIds: new[] { Guid.NewGuid() });

    [Fact]
    public async Task Handle_ShouldCallRegisterProvisioned_AndCreateProfile_OnHappyPath()
    {
        var userId    = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        _users.RegisterProvisionedAsync(Arg.Any<InvitedUserRegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(userId));

        InvitedProfileCreationRequest? profileReq = null;
        _profiles.CreateForInvitedUserAsync(
                Arg.Do<InvitedProfileCreationRequest>(r => profileReq = r),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(profileId));

        var sut = CreateSut();

        var result = await sut.Handle(Command("New.User@Example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.UserId.Should().Be(userId);
        result.Value.ProfileId.Should().Be(profileId);

        // Email was normalized (lowercased, trimmed) before calling Security.
        await _users.Received(1).RegisterProvisionedAsync(
            Arg.Is<InvitedUserRegistrationRequest>(r =>
                r.Email == "new.user@example.com"
             && r.FirstName == "Jo"
             && r.LastName  == "Doe"),
            Arg.Any<CancellationToken>());

        // Profile was linked to the newly-created user and display/avatar
        // were trimmed to non-empty values.
        profileReq.Should().NotBeNull();
        profileReq!.UserId.Should().Be(userId);
        profileReq.DisplayName.Should().Be("Jo D.");
        profileReq.AvatarUrl.Should().Be("https://cdn/a.png");
    }

    [Fact]
    public async Task Handle_ShouldNormalizeEmptyDisplayAndAvatar_ToNull()
    {
        var userId    = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        _users.RegisterProvisionedAsync(Arg.Any<InvitedUserRegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(userId));

        InvitedProfileCreationRequest? profileReq = null;
        _profiles.CreateForInvitedUserAsync(
                Arg.Do<InvitedProfileCreationRequest>(r => profileReq = r),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(profileId));

        var command = new ProvisionAccountCommand(
            Email:          "a@b.com",
            FirstName:      "Jo",
            LastName:       "Doe",
            DisplayName:    "   ",
            AvatarUrl:      "",
            InitialRoleIds: new[] { Guid.NewGuid() });

        var sut = CreateSut();

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profileReq!.DisplayName.Should().BeNull("whitespace-only display name must not be persisted verbatim");
        profileReq.AvatarUrl.Should().BeNull("empty avatar URL must collapse to null");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRegisterProvisionedFails_AndNotCreateProfile()
    {
        _users.RegisterProvisionedAsync(Arg.Any<InvitedUserRegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Conflict(Error.Conflict("User.Email", "exists")));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        await _profiles.DidNotReceive().CreateForInvitedUserAsync(
            Arg.Any<InvitedProfileCreationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProfileCreationFails()
    {
        var userId = Guid.NewGuid();

        _users.RegisterProvisionedAsync(Arg.Any<InvitedUserRegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(userId));

        _profiles.CreateForInvitedUserAsync(Arg.Any<InvitedProfileCreationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure(Error.Failure("Profile.Failed", "bad"), Outcome.ServerError));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);
    }
}
