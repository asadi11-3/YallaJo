using Accounts.Contracts.Abstractions;
using Auth.Application.Commands.Register;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserRegistrationService _userReg         = Substitute.For<IUserRegistrationService>();
    private readonly IProfileCreationService  _profileCreation = Substitute.For<IProfileCreationService>();
    private readonly IOtpRepository           _otpRepo         = Substitute.For<IOtpRepository>();
    private readonly IAuthUnitOfWork          _uow             = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService              _otpService      = Substitute.For<IOtpService>();
    private readonly IEmailService            _email           = Substitute.For<IEmailService>();
    private readonly IAuthOutboxWriter        _outbox          = Substitute.For<IAuthOutboxWriter>();
    private readonly TimeProvider             _timeProvider    = TimeProvider.System;

    private RegisterCommandHandler CreateSut() =>
        new(_userReg, _profileCreation, _otpRepo, _uow, _otpService, _email, _outbox, _timeProvider,
            NullLogger<RegisterCommandHandler>.Instance);

    private static RegisterCommand SampleCommand(string email = "new@example.com") =>
        new("Joe", "Doe", email, "SuperSecret123", "test-recaptcha-token");

    private void ArrangeHappySecurityAndAccounts(Guid userId)
    {
        _userReg.RegisterAsync(Arg.Any<UserRegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(userId));
        _profileCreation.CreateForUserAsync(Arg.Any<ProfileCreationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(Guid.NewGuid()));
        _otpService.Generate().Returns("123456");
        _otpService.Hash(Arg.Any<string>()).Returns("hash");
    }

    [Fact]
    public async Task Handle_ShouldReturnCreated_AndSendEmail_OnHappyPath()
    {
        var userId = Guid.NewGuid();
        ArrangeHappySecurityAndAccounts(userId);

        var sut = CreateSut();

        var result = await sut.Handle(SampleCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.UserId.Should().Be(userId);

        await _otpRepo.Received(1).AddAsync(Arg.Any<Otp>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync(
            "new@example.com",
            Arg.Is<string>(s => s.Contains("Verify", StringComparison.OrdinalIgnoreCase)),
            Arg.Is<string>(b => b.Contains("123456", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPropagateFailure_WhenSecurityRegistrationFails()
    {
        _userReg.RegisterAsync(Arg.Any<UserRegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Conflict(Error.Conflict("User.Email", "exists")));

        var sut = CreateSut();

        var result = await sut.Handle(SampleCommand("dup@example.com"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _otpRepo.DidNotReceive().AddAsync(Arg.Any<Otp>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldTolerateProfileConflict_AndStillProceedWithOtpEmail()
    {
        var userId = Guid.NewGuid();
        _userReg.RegisterAsync(Arg.Any<UserRegistrationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Created(userId));
        _profileCreation.CreateForUserAsync(Arg.Any<ProfileCreationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Conflict(Error.Conflict("Profile.UserId", "exists")));
        _otpService.Generate().Returns("654321");
        _otpService.Hash(Arg.Any<string>()).Returns("h");

        var sut = CreateSut();

        var result = await sut.Handle(SampleCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _email.Received(1).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnEmailFailure_ShouldReturn500_AndInvalidateOtp()
    {
        var userId = Guid.NewGuid();
        ArrangeHappySecurityAndAccounts(userId);

        Otp? captured = null;
        _otpRepo.AddAsync(Arg.Do<Otp>(o => captured = o), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException("smtp down")));

        var sut = CreateSut();

        var result = await sut.Handle(SampleCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);
        result.Errors.Should().ContainSingle(e => e.Code == "Registration.EmailDeliveryFailed");

        captured.Should().NotBeNull();
        captured!.IsUsed.Should().BeTrue("a verification OTP that could not be emailed must not remain active");

        // Two SaveChanges: one for OTP insertion, one for OTP invalidation.
        await _uow.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNormalizeEmail_WhenSending()
    {
        ArrangeHappySecurityAndAccounts(Guid.NewGuid());

        var sut = CreateSut();

        await sut.Handle(SampleCommand("  MixedCase@Example.COM  "), CancellationToken.None);

        await _email.Received(1).SendAsync(
            "mixedcase@example.com",
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
