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
/// Security-focused tests for <see cref="ExternalLoginCommandHandler"/>. The
/// ExternalLogin path MUST NEVER:
/// <list type="bullet">
///   <item><description>Auto-provision an account from an untrusted email.</description></item>
///   <item><description>Accept an unverified / tampered / expired ticket.</description></item>
///   <item><description>Re-accept a consumed ticket.</description></item>
///   <item><description>Sign in a user whose Security record is deactivated or
///   whose email has not been verified on this platform.</description></item>
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
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IExternalAuthTicketVerifier _verifier = Substitute.For<IExternalAuthTicketVerifier>();
    private readonly IExternalAuthNonceStore _nonce = Substitute.For<IExternalAuthNonceStore>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();

    private ExternalLoginCommandHandler CreateSut() =>
        new(_extRepo, _deviceRepo, _sessionRepo, _refreshRepo, _uow,
            _security, _tokens, _verifier, _nonce, _requestContext,
            NullLogger<ExternalLoginCommandHandler>.Instance);

    private static ExternalAuthTicket SampleTicket(string provider = "google", string providerUserId = "g-1") =>
        new(
            TicketId: Guid.NewGuid(),
            Provider: provider,
            ProviderUserId: providerUserId,
            Email: "user@gmail.com",
            EmailVerifiedByProvider: true,
            IssuedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(2));

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

    [Fact]
    public async Task Handle_ShouldReject_WhenNoActiveLink_NoAutoProvision()
    {
        var ticket = SampleTicket();
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns((ExternalProvider?)null);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _security.DidNotReceiveWithAnyArgs().GetUserDataByIdAsync(default, default);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenSecurityUserMissing()
    {
        var ticket = SampleTicket();
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var link = ExternalProvider.Create(Guid.NewGuid(), "google", "g-1", "a@b.c");
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns(link);
        _security.GetUserDataByIdAsync(link.UserId, Arg.Any<CancellationToken>())
            .Returns((SecurityUserData?)null);

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenEmailNotVerifiedOnPlatform()
    {
        var ticket = SampleTicket();
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var userId = Guid.NewGuid();
        var link = ExternalProvider.Create(userId, "google", "g-1", "a@b.c");
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns(link);

        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId,
                Email: "a@b.c",
                IsEmailVerified: false,
                Roles: Array.Empty<string>(),
                Claims: Array.Empty<(string, string)>()));

        var sut = CreateSut();

        var result = await sut.Handle(new ExternalLoginCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldCreateSessionAndTokens_OnHappyPath()
    {
        var ticket = SampleTicket();
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var userId = Guid.NewGuid();
        var link = ExternalProvider.Create(userId, "google", "g-1", "a@b.c");
        _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
            .Returns(link);

        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId,
                Email: "a@b.c",
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
        result.Value.AccessToken.Should().Be("jwt-access");
        result.Value.RefreshToken.Should().Be("plain-refresh");

        await _deviceRepo.Received(1).AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>());
        await _sessionRepo.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _refreshRepo.Received(1).AddAsync(Arg.Any<Auth.Domain.Entities.RefreshToken>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
