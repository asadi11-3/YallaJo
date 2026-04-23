using System.Linq.Expressions;
using Accounts.Contracts.Abstractions;
using Auth.Application.Commands.ExternalLogin;
using Auth.Application.ExternalAuth;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Covers Scenario A of the external-login spec: first-time provider login
/// when NO local account exists with the provider-asserted email.
///
/// <para>Production behavior: provision a brand-new identity (Security user +
/// Accounts profile), attach the provider link, then sign the user in — all
/// in one round trip, same UX as Google/Meta/GitHub.</para>
///
/// <para>Safety invariants (all must hold for auto-create to proceed):</para>
/// <list type="bullet">
///   <item><description>Ticket is valid and not replayed.</description></item>
///   <item><description>Ticket carries an email AND
///   <c>EmailVerifiedByProvider = true</c>.</description></item>
///   <item><description>No existing local account with that email (lookup
///   returns null).</description></item>
///   <item><description>No other user owns this provider identity (defense
///   in depth).</description></item>
///   <item><description>Provision succeeded AND Accounts profile row
///   committed (or already existed = tolerated).</description></item>
/// </list>
///
/// <para>Any failure = generic 401, fail-closed.</para>
/// </summary>
public sealed class ExternalLoginAutoCreateTests
{
    private readonly IExternalProviderRepository _extRepo = Substitute.For<IExternalProviderRepository>();
    private readonly IDeviceRepository _deviceRepo = Substitute.For<IDeviceRepository>();
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly IRefreshTokenRepository _refreshRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthUnitOfWork _uow = Substitute.For<IAuthUnitOfWork>();
    private readonly ISecurityService _security = Substitute.For<ISecurityService>();
    private readonly IUserRegistrationService _registration = Substitute.For<IUserRegistrationService>();
    private readonly IProfileCreationService _profile = Substitute.For<IProfileCreationService>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IExternalAuthTicketVerifier _verifier = Substitute.For<IExternalAuthTicketVerifier>();
    private readonly IExternalAuthNonceStore _nonce = Substitute.For<IExternalAuthNonceStore>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();

    private ExternalLoginCommandHandler CreateSut() =>
        new(_extRepo, _deviceRepo, _sessionRepo, _refreshRepo, _uow,
            _security, _registration, _profile, _tokens, _verifier, _nonce, _requestContext,
            NullLogger<ExternalLoginCommandHandler>.Instance);

    private static ExternalAuthTicket SampleTicket(
        string? email = "new.user@gmail.com",
        bool emailVerifiedByProvider = true,
        string? firstName = "New",
        string? lastName = "User") =>
        new(
            TicketId: Guid.NewGuid(),
            Provider: "google",
            ProviderUserId: "g-new-1",
            Email: email,
            EmailVerifiedByProvider: emailVerifiedByProvider,
            IssuedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(2),
            FirstName: firstName,
            LastName: lastName);

    private void SetupValidTicketAndNonce_NoLinkNoLocalAccount(ExternalAuthTicket ticket)
    {
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _extRepo.FindActiveLinkAsync("google", "g-new-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);
        _security.GetUserIdByEmailAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
    }

    [Fact]
    public async Task Handle_ShouldAutoCreate_AndSignIn_WhenNoLocalAccountAndProviderVerifiesEmail()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);

        // Defense-in-depth "provider identity already owned by another user"
        // check must return false for the happy path.
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var newUserId = Guid.NewGuid();
        _registration.RegisterExternalAsync(
                Arg.Any<ExternalUserRegistrationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(newUserId));
        _profile.CreateForUserAsync(
                Arg.Any<ProfileCreationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(Guid.NewGuid()));

        _security.GetUserDataByIdAsync(newUserId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId:          newUserId,
                Email:           "new.user@gmail.com",
                IsEmailVerified: true,
                Roles:           new[] { "User" },
                Claims:          Array.Empty<(string, string)>()));

        ExternalProvider? capturedLink = null;
        _extRepo.AddAsync(Arg.Do<ExternalProvider>(ep => capturedLink = ep), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _tokens.GenerateRefreshToken().Returns("plain-refresh");
        _tokens.HashRefreshToken("plain-refresh").Returns("hash");
        _tokens.GenerateAccessToken(Arg.Any<TokenData>()).Returns("jwt-access");

        var sut = CreateSut();

        var result = await sut.Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(newUserId);
        result.Value.AccessToken.Should().Be("jwt-access");

        // Security user registered with verified + active semantics.
        await _registration.Received(1).RegisterExternalAsync(
            Arg.Is<ExternalUserRegistrationRequest>(r =>
                r.Email == "new.user@gmail.com"
             && r.FirstName == "New"
             && r.LastName == "User"),
            Arg.Any<CancellationToken>());

        // Accounts profile provisioned.
        await _profile.Received(1).CreateForUserAsync(
            Arg.Is<ProfileCreationRequest>(r =>
                r.UserId == newUserId
             && r.FirstName == "New"
             && r.LastName == "User"),
            Arg.Any<CancellationToken>());

        // Provider link attached to the brand-new user.
        capturedLink.Should().NotBeNull();
        capturedLink!.UserId.Should().Be(newUserId);
        capturedLink.Provider.Should().Be("google");
        capturedLink.ProviderUserId.Should().Be(ticket.ProviderUserId);
        capturedLink.IsActive.Should().BeTrue();

        // Session/device/refresh token created in one Auth UoW commit.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _deviceRepo.Received(1).AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>());
        await _sessionRepo.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _refreshRepo.Received(1).AddAsync(Arg.Any<Auth.Domain.Entities.RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldTolerateProfileAlreadyExists_WhenAccountsReturnsConflict()
    {
        // Profile may already exist on a retry; Accounts returning Conflict
        // is idempotent from the auth flow's POV and must NOT break sign-in.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var newUserId = Guid.NewGuid();
        _registration.RegisterExternalAsync(
                Arg.Any<ExternalUserRegistrationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(newUserId));
        _profile.CreateForUserAsync(
                Arg.Any<ProfileCreationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Conflict(
                Error.Conflict("Profile.UserId", "Profile already exists for this user.")));

        _security.GetUserDataByIdAsync(newUserId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                newUserId, "new.user@gmail.com", true,
                new[] { "User" }, Array.Empty<(string, string)>()));

        _tokens.GenerateRefreshToken().Returns("r");
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns("h");
        _tokens.GenerateAccessToken(Arg.Any<TokenData>()).Returns("a");

        var sut = CreateSut();

        var result = await sut.Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoCreate_WhenProviderDidNotVerifyEmail()
    {
        var ticket = SampleTicket(emailVerifiedByProvider: false);
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);

        var result = await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _registration.DidNotReceiveWithAnyArgs().RegisterExternalAsync(default!, default);
        await _profile.DidNotReceiveWithAnyArgs().CreateForUserAsync(default!, default);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoCreate_WhenTicketHasNoEmail()
    {
        var ticket = SampleTicket(email: null);
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);

        var result = await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _registration.DidNotReceiveWithAnyArgs().RegisterExternalAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoCreate_WhenProviderIdentityAlreadyTaken()
    {
        // Defense in depth — the existing-link probe already returned null,
        // but between those two queries someone else inserted the same
        // (provider, providerUserId). We must refuse rather than create a
        // second owner.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _registration.DidNotReceiveWithAnyArgs().RegisterExternalAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoCreate_WhenRegistrationServiceReturnsConflict()
    {
        // The "concurrent race" case — another request just created the
        // same email. Fail closed.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _registration.RegisterExternalAsync(
                Arg.Any<ExternalUserRegistrationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists.")));

        var result = await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        await _profile.DidNotReceiveWithAnyArgs().CreateForUserAsync(default!, default);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoCreate_WhenProfileCreationHardFails()
    {
        // Security user was created but Accounts profile creation hard
        // failed (non-conflict). We log loudly and refuse the sign-in so
        // the operator sees a half-provisioned account before serving
        // tokens for it.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var newUserId = Guid.NewGuid();
        _registration.RegisterExternalAsync(
                Arg.Any<ExternalUserRegistrationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(newUserId));

        _profile.CreateForUserAsync(
                Arg.Any<ProfileCreationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure(
                Error.Failure("Profile.Unknown", "DB error"),
                Outcome.ServerError));

        var result = await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldPassProviderNames_ThroughToRegistrationAndProfile()
    {
        // Critical for the "no half-created users" rule — the FirstName /
        // LastName from the provider must flow from Web → ticket → API →
        // Security + Accounts so Profile is NOT seeded with blanks.
        var ticket = SampleTicket(firstName: "Ada", lastName: "Lovelace");
        SetupValidTicketAndNonce_NoLinkNoLocalAccount(ticket);
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var newUserId = Guid.NewGuid();
        _registration.RegisterExternalAsync(
                Arg.Any<ExternalUserRegistrationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(newUserId));
        _profile.CreateForUserAsync(
                Arg.Any<ProfileCreationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(Guid.NewGuid()));
        _security.GetUserDataByIdAsync(newUserId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                newUserId, "new.user@gmail.com", true,
                Array.Empty<string>(), Array.Empty<(string, string)>()));
        _tokens.GenerateRefreshToken().Returns("r");
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns("h");
        _tokens.GenerateAccessToken(Arg.Any<TokenData>()).Returns("a");

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        await _registration.Received(1).RegisterExternalAsync(
            Arg.Is<ExternalUserRegistrationRequest>(r =>
                r.FirstName == "Ada" && r.LastName == "Lovelace"),
            Arg.Any<CancellationToken>());

        await _profile.Received(1).CreateForUserAsync(
            Arg.Is<ProfileCreationRequest>(r =>
                r.FirstName == "Ada" && r.LastName == "Lovelace"),
            Arg.Any<CancellationToken>());
    }
}
