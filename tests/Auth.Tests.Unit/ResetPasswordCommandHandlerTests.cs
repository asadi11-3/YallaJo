using System.Linq.Expressions;
using Auth.Application.Commands.ResetPassword;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Tests.Unit.TestDoubles;
using FluentAssertions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Covers the cross-module consistency fix: ResetPassword now wraps the
/// Security password reset + the Auth OTP burn in a single ambient
/// TransactionScope. When Security reports failure, NO Auth state may be
/// committed — so the OTP stays claimable for a new attempt rather than
/// being burned for nothing.
/// </summary>
public sealed class ResetPasswordCommandHandlerTests
{
    private readonly ISecurityService _security   = Substitute.For<ISecurityService>();
    private readonly IOtpRepository   _otpRepo    = Substitute.For<IOtpRepository>();
    private readonly IAuthUnitOfWork  _uow        = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService      _otpService = Substitute.For<IOtpService>();
    private readonly PassThroughTransactionalExecutor _tx = new();

    private ResetPasswordCommandHandler CreateSut() =>
        new(_security, _otpRepo, _uow, _otpService, _tx);

    private static ResetPasswordCommand Command(string email = "user@example.com") =>
        new(email, "123456", "NewPass123", "NewPass123");

    [Fact]
    public async Task Handle_ShouldFail_WhenUserNotFound()
    {
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_OnSecurityResetFailure_ShouldReturn500_AndNotSaveAuthState()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var otp = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(otp);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _security.ResetPasswordAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);

        // OTP was marked used in-memory (handler mutation before the scope),
        // but SaveChangesAsync MUST NOT have been called — the ambient tx
        // was never Completed so the dispose rolls back any enlisted work.
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldCallBothSecurityAndAuthSaveChanges()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var otp = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(otp);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _security.ResetPasswordAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        otp.IsUsed.Should().BeTrue();

        await _security.Received(1).ResetPasswordAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnInvalidOtp_ShouldNotTouchSecurity_AndShouldPersistAttemptOnly()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var otp = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(otp);

        _otpService.Verify("123456", "hash").Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);

        await _security.DidNotReceive()
            .ResetPasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        // The increment-attempt SaveChanges happens OUTSIDE the transaction scope.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        otp.AttemptCount.Should().Be(1);
    }
}
