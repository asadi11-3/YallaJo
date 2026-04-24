using Auth.Application.Commands.Login;
using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2A — verifies that <see cref="LoginCommandHandler"/> rejects login
/// attempts for any account whose lifecycle state is not
/// <see cref="AccountLifecycleSnapshot.Active"/>, even when the password
/// hash matches and the email is verified.
/// </summary>
public sealed class LoginCommandHandlerLifecycleGateTests
{
    private readonly ISecurityService        _security        = Substitute.For<ISecurityService>();
    private readonly IDeviceRepository       _deviceRepo      = Substitute.For<IDeviceRepository>();
    private readonly ISessionRepository      _sessionRepo     = Substitute.For<ISessionRepository>();
    private readonly IRefreshTokenRepository _refreshRepo     = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthUnitOfWork         _uow             = Substitute.For<IAuthUnitOfWork>();
    private readonly ITokenService           _tokenService    = Substitute.For<ITokenService>();
    private readonly IRequestContext         _requestContext  = Substitute.For<IRequestContext>();

    private LoginCommandHandler CreateSut() =>
        new(_security, _deviceRepo, _sessionRepo, _refreshRepo, _uow, _tokenService, _requestContext);

    private static LoginCommand Command() => new("user@example.com", "Pa55word!", "test-recaptcha-token");

    [Theory]
    [InlineData(AccountLifecycleSnapshot.Provisioned)]
    [InlineData(AccountLifecycleSnapshot.PendingActivation)]
    [InlineData(AccountLifecycleSnapshot.Suspended)]
    [InlineData(AccountLifecycleSnapshot.PendingPasswordReset)]
    [InlineData(AccountLifecycleSnapshot.Archived)]
    public async Task Handle_ShouldReject_WhenLifecycleStateIsNotActive(
        AccountLifecycleSnapshot lifecycle)
    {
        var userId = Guid.NewGuid();
        _security.VerifyCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId:          userId,
                Email:           "user@example.com",
                IsEmailVerified: true,
                Roles:           Array.Empty<string>(),
                Claims:          Array.Empty<(string, string)>(),
                Lifecycle:       lifecycle));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);

        // No session / refresh-token / device row may be persisted for a
        // rejected login — the lifecycle gate runs BEFORE any Auth-side
        // mutation.
        await _sessionRepo.DidNotReceive().AddAsync(Arg.Any<Auth.Domain.Entities.Session>(), Arg.Any<CancellationToken>());
        await _refreshRepo.DidNotReceive().AddAsync(Arg.Any<Auth.Domain.Entities.RefreshToken>(), Arg.Any<CancellationToken>());
        await _deviceRepo.DidNotReceive().AddAsync(Arg.Any<Auth.Domain.Entities.Device>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldStillReject_WhenEmailUnverified_RegardlessOfLifecycleState()
    {
        // Email-verification gate runs before the lifecycle gate; the message
        // it produces is intentionally distinct so admins can diagnose stuck
        // accounts. Both gates share the same Outcome.Unauthorized.
        var userId = Guid.NewGuid();
        _security.VerifyCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId:          userId,
                Email:           "user@example.com",
                IsEmailVerified: false,
                Roles:           Array.Empty<string>(),
                Claims:          Array.Empty<(string, string)>(),
                Lifecycle:       AccountLifecycleSnapshot.Active));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }
}
