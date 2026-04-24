using System.Linq.Expressions;
using Auth.Application.Commands.ForgotPassword;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Covers Phase 1 semantics:
///   • Lifecycle gate — recovery is allowed only for Active, email-verified
///     accounts. Non-existent, un-activated, suspended, or unverified-email
///     accounts all return the same generic success with NO side effects.
///   • Honest failure on SMTP — when the email provider throws, the OTP is
///     NEVER persisted (previously the row was created-then-MarkedUsed,
///     stamping UsedAt on a token that was never consumed). The response
///     stays enumeration-safe.
///   • Throttle and prior-OTP invalidation are preserved.
/// </summary>
public sealed class ForgotPasswordCommandHandlerTests
{
    private const string GenericMessage = "If this email exists, a reset code was sent.";

    private readonly ISecurityService _security   = Substitute.For<ISecurityService>();
    private readonly IOtpRepository   _otpRepo    = Substitute.For<IOtpRepository>();
    private readonly IAuthUnitOfWork  _uow        = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService      _otpService = Substitute.For<IOtpService>();
    private readonly IEmailService    _email      = Substitute.For<IEmailService>();

    private ForgotPasswordCommandHandler CreateSut() =>
        new(_security, _otpRepo, _uow, _otpService, _email,
            NullLogger<ForgotPasswordCommandHandler>.Instance);

    private static ForgotPasswordCommand Command(string email = "user@example.com")
        => new(email, "test-recaptcha-token");

    private void StubActiveVerifiedAccount(Guid userId, string email = "user@example.com")
    {
        _security.GetAccountStatusByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AccountStatus(userId, email, IsActive: true, IsEmailVerified: true));
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericSuccess_AndSkipSend_WhenUserDoesNotExist()
    {
        _security.GetAccountStatusByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AccountStatus?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command("ghost@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        await _otpRepo.DidNotReceive().AddAsync(Arg.Any<Otp>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, true)]   // not active (e.g. provisioned but never activated, or suspended)
    [InlineData(true,  false)]  // email unverified
    [InlineData(false, false)]  // neither
    public async Task Handle_ShouldReturnGenericSuccess_AndSkipSend_WhenAccountIsNotEligible(
        bool isActive, bool isEmailVerified)
    {
        var userId = Guid.NewGuid();
        _security.GetAccountStatusByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AccountStatus(userId, "user@example.com", isActive, isEmailVerified));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        // No recovery artefacts for ineligible accounts — and no enumeration
        // signal via timing or status code.
        await _otpRepo.DidNotReceive().AddAsync(Arg.Any<Otp>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericSuccess_OnSmtpFailure_AndNeverPersistOtp()
    {
        var userId = Guid.NewGuid();
        StubActiveVerifiedAccount(userId);

        // No recent OTP in throttle window, no existing active OTPs.
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((Otp?)null);

        _otpService.Generate().Returns("111222");
        _otpService.Hash(Arg.Any<string>()).Returns("hash");

        _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException("SMTP DOWN")));

        var sut = CreateSut();

        var result = await sut.Handle(Command("real@example.com"), CancellationToken.None);

        // Enumeration safety: same message, same success status as the "user does not exist" branch.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        // Phase 1 fix: no token is ever persisted when email delivery fails —
        // eliminates the MarkUsed-on-failure lie that stamped UsedAt on a
        // token that was never consumed.
        await _otpRepo.DidNotReceive().AddAsync(Arg.Any<Otp>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrottle_WhenRecentOtpIsWithin60s_ReturningGenericSuccess()
    {
        var userId = Guid.NewGuid();
        StubActiveVerifiedAccount(userId);

        // Simulate an OTP created 10 seconds ago.
        var recent = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(recent);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        await _otpRepo.DidNotReceive().AddAsync(Arg.Any<Otp>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSendEmail_ThenPersistOtp_AndInvalidatePriorActiveOtps()
    {
        var userId = Guid.NewGuid();
        StubActiveVerifiedAccount(userId);

        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((Otp?)null);

        var old1 = Otp.Create(userId, "PasswordReset", "h1", "Email", "user@example.com");
        var old2 = Otp.Create(userId, "PasswordReset", "h2", "Email", "user@example.com");
        _otpRepo.GetAllAsync(
            Arg.Any<Expression<Func<Otp, bool>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns([old1, old2]);

        _otpService.Generate().Returns("909090");
        _otpService.Hash(Arg.Any<string>()).Returns("hash");

        Otp? persisted = null;
        _otpRepo.AddAsync(Arg.Do<Otp>(o => persisted = o), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Email was dispatched with the generated plaintext code.
        await _email.Received(1).SendAsync(
            "user@example.com", Arg.Any<string>(),
            Arg.Is<string>(b => b.Contains("909090", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());

        // Prior active OTPs were invalidated.
        old1.IsUsed.Should().BeTrue();
        old2.IsUsed.Should().BeTrue();

        // And a fresh OTP was persisted (only after the email succeeded).
        persisted.Should().NotBeNull();
        persisted!.IsUsed.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
