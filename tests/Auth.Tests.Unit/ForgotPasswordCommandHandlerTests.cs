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
        => new(email);

    [Fact]
    public async Task Handle_ShouldReturnGenericSuccess_AndSkipSend_WhenUserDoesNotExist()
    {
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command("ghost@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        await _otpRepo.DidNotReceive().AddAsync(Arg.Any<Otp>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericSuccess_OnSmtpFailure_AndInvalidateOtp()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        // No recent OTP in throttle window, no existing active OTPs.
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns((Otp?)null);

        _otpRepo.GetAllAsync(
            Arg.Any<Expression<Func<Otp, bool>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<Otp>());

        _otpService.Generate().Returns("111222");
        _otpService.Hash(Arg.Any<string>()).Returns("hash");

        Otp? captured = null;
        _otpRepo.AddAsync(Arg.Do<Otp>(o => captured = o), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException("SMTP DOWN")));

        var sut = CreateSut();

        var result = await sut.Handle(Command("real@example.com"), CancellationToken.None);

        // Enumeration safety: same message, same success status as the "user does not exist" branch.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        captured.Should().NotBeNull();
        captured!.IsUsed.Should().BeTrue("an OTP that was never delivered must not remain usable");
    }

    [Fact]
    public async Task Handle_ShouldThrottle_WhenRecentOtpIsWithin60s_ReturningGenericSuccess()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        // Simulate an OTP created 10 seconds ago.
        var recent = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        // Otp.CreatedAt is set by BaseEntity at construction — use it as-is since Create() uses UtcNow.
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
    public async Task Handle_ShouldInvalidatePriorActiveOtps_BeforeIssuingNewOne()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

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
            .Returns(new List<Otp> { old1, old2 });

        _otpService.Generate().Returns("909090");
        _otpService.Hash(Arg.Any<string>()).Returns("hash");

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        old1.IsUsed.Should().BeTrue();
        old2.IsUsed.Should().BeTrue();

        await _email.Received(1).SendAsync(
            "user@example.com", Arg.Any<string>(),
            Arg.Is<string>(b => b.Contains("909090", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }
}
