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
/// Security-focused tests for <see cref="ExternalLoginCommandHandler"/>.
///
/// <para>The ExternalLogin path MUST NEVER:</para>
/// <list type="bullet">
///   <item><description>Auto-provision an account from an untrusted email.</description></item>
///   <item><description>Auto-link based on a provider-asserted email that the
///   provider itself did not verify.</description></item>
///   <item><description>Auto-link against a local account whose email is not
///   verified on this platform.</description></item>
///   <item><description>Accept an unverified / tampered / expired ticket.</description></item>
///   <item><description>Re-accept a consumed ticket.</description></item>
///   <item><description>Sign in a user whose Security record is deactivated or
///   whose email has not been verified on this platform.</description></item>
///   <item><description>Replace or duplicate an existing provider link on a
///   user via the auto-link path.</description></item>
///   <item><description>Steal a provider identity already linked to someone
///   else, even if emails appear to match.</description></item>
/// </list>
/// </summary>
public sealed class ExternalLoginCommandHandlerTests
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
        string provider = "google",
        string providerUserId = "g-1",
        string? email = "user@gmail.com",
        bool emailVerifiedByProvider = true) =>
        new(
            TicketId: Guid.NewGuid(),
            Provider: provider,
            ProviderUserId: providerUserId,
            Email: email,
            EmailVerifiedByProvider: emailVerifiedByProvider,
            IssuedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(2));

    private void SetupValidTicketAndNonce(ExternalAuthTicket ticket)
    {
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private void SetupLocalUserWithVerifiedEmail(Guid userId, string email)
    {
        _security.GetUserIdByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(userId);
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId,
                Email: email,
                IsEmailVerified: true,
                Roles: new[] { "User" },
                Claims: Array.Empty<(string, string)>()));
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenTicketInvalid()
    {
        _verifier.Verify(Arg.Any<string>())
            .Returns(Result<ExternalAuthTicket>.Failure(
                Error.Unauthorized("bad"), Outcome.Unauthorized));

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _extRepo.DidNotReceiveWithAnyArgs().FindActiveLinkAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReject_OnReplay()
    {
        var ticket = SampleTicket();
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(ticket.TicketId, ticket.ExpiresAt, Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _extRepo.DidNotReceiveWithAnyArgs().FindActiveLinkAsync(default!, default!, default);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Auto-link refusal conditions — every one of these paths must fail closed.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldRefuseAutoLink_WhenTicketHasNoEmail()
    {
        var ticket = SampleTicket(email: null);
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _security.DidNotReceiveWithAnyArgs().GetUserIdByEmailAsync(default!, default);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoLink_WhenProviderDidNotVerifyEmail()
    {
        var ticket = SampleTicket(emailVerifiedByProvider: false);
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        // Must NOT touch local-user lookup or link storage when provider
        // itself has not attested verification.
        await _security.DidNotReceiveWithAnyArgs().GetUserIdByEmailAsync(default!, default);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenRegistrationReturnsConflict()
    {
        // No local account → auto-create. If RegisterExternalAsync returns
        // Conflict (concurrent creation race) we fail closed — no silent
        // merge.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        _registration.RegisterExternalAsync(
                Arg.Any<ExternalUserRegistrationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists.")));

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoLink_WhenLocalEmailNotVerifiedOnPlatform()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns(userId);
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId,
                Email: "user@gmail.com",
                IsEmailVerified: false,
                Roles: Array.Empty<string>(),
                Claims: Array.Empty<(string, string)>()));

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoLink_WhenSecurityUserIsMissingOrDeactivated()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns(userId);
        // Deactivated users surface as null from GetUserDataByIdAsync.
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((SecurityUserData?)null);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoLink_WhenUserAlreadyHasDifferentLinkOnSameProvider()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var userId = Guid.NewGuid();
        SetupLocalUserWithVerifiedEmail(userId, "user@gmail.com");

        // First AnyAsync — "does this user already have an active google link?" → true.
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldRefuseAutoLink_WhenProviderIdentityAlreadyTakenByAnotherUser()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var userId = Guid.NewGuid();
        SetupLocalUserWithVerifiedEmail(userId, "user@gmail.com");

        // First AnyAsync (same-provider self-link) → false, second (taken-by-other) → true.
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false, true);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Happy paths
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldAutoLinkAndSignIn_WhenAllConditionsSafe()
    {
        // "First-time Google login on an account the user already owns via
        // email+password" — the canonical scenario the feature was introduced
        // to fix.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var userId = Guid.NewGuid();
        SetupLocalUserWithVerifiedEmail(userId, "user@gmail.com");

        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        ExternalProvider? captured = null;
        _extRepo.AddAsync(Arg.Do<ExternalProvider>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _tokens.GenerateRefreshToken().Returns("plain-refresh");
        _tokens.HashRefreshToken("plain-refresh").Returns("hash");
        _tokens.GenerateAccessToken(Arg.Any<TokenData>()).Returns("jwt-access");

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(userId);
        result.Value.AccessToken.Should().Be("jwt-access");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(userId);
        captured.Provider.Should().Be("google");
        captured.ProviderUserId.Should().Be(ticket.ProviderUserId);
        captured.IsActive.Should().BeTrue();

        // Auto-linked row + session/device/refresh must all persist inside
        // the SAME unit of work so a crash cannot create a dangling link.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _deviceRepo.Received(1).AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>());
        await _sessionRepo.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _refreshRepo.Received(1).AddAsync(Arg.Any<Auth.Domain.Entities.RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSignIn_WithoutNewLink_WhenProviderAlreadyLinked()
    {
        // "Second Google login" — the existing link is used directly, no new
        // row is added.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);

        var userId = Guid.NewGuid();
        var link = ExternalProvider.Create(userId, "google", "g-1", "user@gmail.com");
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns(link);

        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId,
                Email: "user@gmail.com",
                IsEmailVerified: true,
                Roles: new[] { "User" },
                Claims: Array.Empty<(string, string)>()));

        _tokens.GenerateRefreshToken().Returns("plain-refresh");
        _tokens.HashRefreshToken("plain-refresh").Returns("hash");
        _tokens.GenerateAccessToken(Arg.Any<TokenData>()).Returns("jwt-access");

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(userId);

        // Critically: no email lookup, no auto-link probe, no new row.
        await _security.DidNotReceiveWithAnyArgs().GetUserIdByEmailAsync(default!, default);
        await _extRepo.DidNotReceiveWithAnyArgs().AnyAsync(
            (Expression<Func<ExternalProvider, bool>>)default!, default);
        await _extRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenLinkedUserEmailIsUnverifiedOnPlatform()
    {
        // Even for an already-linked user, if the platform primary email
        // somehow ended up unverified (e.g. deliberate revocation), login
        // must be refused.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);

        var userId = Guid.NewGuid();
        var link = ExternalProvider.Create(userId, "google", "g-1", "user@gmail.com");
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns(link);

        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId,
                Email: "user@gmail.com",
                IsEmailVerified: false,
                Roles: Array.Empty<string>(),
                Claims: Array.Empty<(string, string)>()));

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
