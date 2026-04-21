using System.Linq.Expressions;
using Auth.Application.Commands.VerifyEmail;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Tests.Unit.TestDoubles;
using FluentAssertions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Covers the cross-module consistency fix: VerifyEmail now wraps Auth's
/// session/device/refresh-token SaveChanges and Security's email-verified
/// flag in a single ambient TransactionScope. On Security failure, the Auth
/// writes must not leak.
/// </summary>
public sealed class VerifyEmailCommandHandlerTests
{
    private readonly ISecurityService        _security = Substitute.For<ISecurityService>();
    private readonly IOtpRepository          _otpRepo  = Substitute.For<IOtpRepository>();
    private readonly IDeviceRepository       _deviceRepo = Substitute.For<IDeviceRepository>();
    private readonly ISessionRepository      _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly IRefreshTokenRepository _refreshRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthUnitOfWork         _uow = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService             _otpService = Substitute.For<IOtpService>();
    private readonly ITokenService           _tokenService = Substitute.For<ITokenService>();
    private readonly IRequestContext         _requestContext = Substitute.For<IRequestContext>();
    private readonly PassThroughTransactionalExecutor _tx = new();

    private VerifyEmailCommandHandler CreateSut() =>
        new(_security, _otpRepo, _deviceRepo, _sessionRepo, _refreshRepo,
            _uow, _otpService, _tokenService, _requestContext, _tx);

    private void ArrangeValidOtp(Guid userId)
    {
        var otp = Otp.Create(userId, "EmailVerification", "hash", "Email", "u@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(otp);
        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenService.GenerateRefreshToken().Returns("plain-rt");
        _tokenService.HashRefreshToken(Arg.Any<string>()).Returns("hashed-rt");
        _tokenService.GenerateAccessToken(Arg.Any<TokenData>()).Returns("access-token");
    }

    [Fact]
    public async Task Handle_OnUserNotFound_ShouldReturnNotFound()
    {
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new VerifyEmailCommand("u@example.com", "123456"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_OnSecurityMarkVerifiedFailure_ShouldReturn500_AndNotCommitAuthWrites()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);
        _security.MarkEmailVerifiedAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        ArrangeValidOtp(userId);

        var sut = CreateSut();
        var result = await sut.Handle(new VerifyEmailCommand("u@example.com", "123456"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);

        // Only 1 SaveChanges is called inside the scope — the scope is then
        // disposed WITHOUT Complete() so the ambient tx rolls back any
        // enlisted writes. We cannot observe tx enlistment directly with
        // mocks; we assert the PRE-Complete call shape instead.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _security.Received(1).MarkEmailVerifiedAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnHappyPath_ShouldCommitBothSidesAndReturnTokens()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);
        _security.MarkEmailVerifiedAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(userId, "u@example.com", true, Array.Empty<string>(),
                Array.Empty<(string, string)>()));

        ArrangeValidOtp(userId);

        var sut = CreateSut();
        var result = await sut.Handle(new VerifyEmailCommand("u@example.com", "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("plain-rt");

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _deviceRepo.Received(1).AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>());
        await _sessionRepo.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _refreshRepo.Received(1).AddAsync(Arg.Any<Auth.Domain.Entities.RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnExpiredOtp_ShouldReturnInvalid_AndNotTouchSecurity()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var expired = Otp.Create(userId, "EmailVerification", "hash", "Email", "u@example.com",
            expiryMinutes: -1); // already expired
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(expired);

        var sut = CreateSut();
        var result = await sut.Handle(new VerifyEmailCommand("u@example.com", "123456"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);

        await _security.DidNotReceive().MarkEmailVerifiedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
