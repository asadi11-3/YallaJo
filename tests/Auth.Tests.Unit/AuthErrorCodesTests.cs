using Auth.Application.Commands.ActivateAccount;
using Auth.Application.Commands.AdminArchiveUser;
using Auth.Application.Commands.AdminReactivateUser;
using Auth.Application.Commands.AdminSuspendUser;
using Auth.Application.Commands.ResetPassword;
using Auth.Application.Commands.VerifyEmail;
using Auth.Application.Errors;
using Auth.Application.Interfaces;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Entities;
using Auth.Domain.Errors;
using Auth.Domain.Repositories;
using Auth.Tests.Unit.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 1 — locks in the centralized Auth error catalogs:
/// <list type="bullet">
///   <item><description><see cref="AuthErrors.UserNotFound"/> (<c>NotFound.User</c>)</description></item>
///   <item><description><see cref="OtpErrors.NotFound"/> (<c>NotFound.Otp</c>)</description></item>
///   <item><description><see cref="InviteErrors.NotFound"/> (<c>NotFound.Invite</c>)</description></item>
///   <item><description><see cref="AuthErrors.AdminUnauthenticated"/> (<c>Auth.Unauthenticated</c>)</description></item>
/// </list>
/// Guards against the previously-shipped double-prefixed codes
/// (<c>NotFound.User.NotFound</c> / <c>NotFound.Otp.NotFound</c> /
/// <c>NotFound.Invite.NotFound</c>) and the inline-literal copy of
/// <c>Auth.Unauthenticated</c> across five admin handlers.
/// </summary>
public sealed class AuthErrorCodesTests
{
    // ── User-not-found ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ShouldReturnUserNotFound_WhenEmailUnknown()
    {
        var security = Substitute.For<ISecurityService>();
        var tokens   = Substitute.For<IPasswordResetTokenRepository>();
        var uow      = Substitute.For<IAuthUnitOfWork>();
        var otp      = Substitute.For<IOtpService>();
        var revoke   = Substitute.For<ISessionRevocationService>();
        var cache    = Substitute.For<HybridCache>();
        var tx       = new PassThroughTransactionalExecutor();

        security
            .GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = new ResetPasswordCommandHandler(security, tokens, uow, otp, tx, revoke, cache);

        var result = await sut.Handle(
            new ResetPasswordCommand("ghost@example.com", "123456", "p", "p"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(AuthErrors.UserNotFound.Code)
            .And.Be("NotFound.User");

        // Regression guard: never the double-prefixed legacy code.
        result.Errors.Should().NotContain(e => e.Code == "NotFound.User.NotFound");
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturnUserNotFound_WhenEmailUnknown()
    {
        var security    = Substitute.For<ISecurityService>();
        var otpRepo     = Substitute.For<IOtpRepository>();
        var deviceRepo  = Substitute.For<IDeviceRepository>();
        var sessionRepo = Substitute.For<ISessionRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var uow         = Substitute.For<IAuthUnitOfWork>();
        var otp         = Substitute.For<IOtpService>();
        var tokens      = Substitute.For<ITokenService>();
        var requestCtx  = Substitute.For<IRequestContext>();
        var tx          = new PassThroughTransactionalExecutor();

        security
            .GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = new VerifyEmailCommandHandler(
            security, otpRepo, deviceRepo, sessionRepo, refreshRepo,
            uow, otp, tokens, requestCtx, tx);

        var result = await sut.Handle(
            new VerifyEmailCommand("ghost@example.com", "000000", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(AuthErrors.UserNotFound.Code)
            .And.Be("NotFound.User");

        result.Errors.Should().NotContain(e => e.Code == "NotFound.User.NotFound");
    }

    // ── OTP not-found ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ShouldReturnOtpNotFound_WhenNoActiveResetToken()
    {
        var userId = Guid.NewGuid();

        var security = Substitute.For<ISecurityService>();
        var tokens   = Substitute.For<IPasswordResetTokenRepository>();
        var uow      = Substitute.For<IAuthUnitOfWork>();
        var otp      = Substitute.For<IOtpService>();
        var revoke   = Substitute.For<ISessionRevocationService>();
        var cache    = Substitute.For<HybridCache>();
        var tx       = new PassThroughTransactionalExecutor();

        security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        tokens.GetLatestActiveForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var sut = new ResetPasswordCommandHandler(security, tokens, uow, otp, tx, revoke, cache);

        var result = await sut.Handle(
            new ResetPasswordCommand("user@example.com", "123456", "p", "p"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OtpErrors.NotFound.Code)
            .And.Be("NotFound.Otp");

        result.Errors.Should().NotContain(e => e.Code == "NotFound.Otp.NotFound");
    }

    // ── Invite not-found ──────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAccount_ShouldReturnInviteNotFound_WhenStatusMissing()
    {
        var users  = Substitute.For<IUserRegistrationService>();
        var tokens = Substitute.For<IActivationTokenRepository>();
        var uow    = Substitute.For<IAuthUnitOfWork>();
        var invite = Substitute.For<IInviteTokenService>();
        var revoke = Substitute.For<ISessionRevocationService>();
        var cache  = Substitute.For<HybridCache>();

        users.GetInviteAccountStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((InviteAccountStatus?)null);

        var sut = new ActivateAccountCommandHandler(users, tokens, uow, invite, revoke, cache);

        var result = await sut.Handle(
            new ActivateAccountCommand("ghost@example.com", "tok", "Pa55word!", "Pa55word!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(InviteErrors.NotFound.Code)
            .And.Be("NotFound.Invite");

        result.Errors.Should().NotContain(e => e.Code == "NotFound.Invite.NotFound");
    }

    // ── Auth.Unauthenticated centralization (sample two admin handlers) ───────

    [Fact]
    public async Task AdminSuspend_ShouldReturnAdminUnauthenticated_WhenActorMissing()
    {
        var security        = Substitute.For<ISecurityService>();
        var revoke          = Substitute.For<ISessionRevocationService>();
        var uow             = Substitute.For<IAuthUnitOfWork>();
        var auditWriter     = Substitute.For<IAdminAuditWriter>();
        var requestContext  = Substitute.For<IRequestContext>();
        var currentUser     = Substitute.For<ICurrentUser>();
        var cache           = Substitute.For<HybridCache>();

        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var sut = new AdminSuspendUserCommandHandler(
            security, revoke, uow, auditWriter, requestContext, currentUser, cache,
            NullLogger<AdminSuspendUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminSuspendUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(AuthErrors.AdminUnauthenticated.Code)
            .And.Be("Auth.Unauthenticated");
    }

    [Fact]
    public async Task AdminArchive_ShouldReturnAdminUnauthenticated_WhenActorMissing()
    {
        var security        = Substitute.For<ISecurityService>();
        var revoke          = Substitute.For<ISessionRevocationService>();
        var uow             = Substitute.For<IAuthUnitOfWork>();
        var auditWriter     = Substitute.For<IAdminAuditWriter>();
        var requestContext  = Substitute.For<IRequestContext>();
        var currentUser     = Substitute.For<ICurrentUser>();
        var cache           = Substitute.For<HybridCache>();

        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var sut = new AdminArchiveUserCommandHandler(
            security, revoke, uow, auditWriter, requestContext, currentUser, cache,
            NullLogger<AdminArchiveUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminArchiveUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(AuthErrors.AdminUnauthenticated.Code)
            .And.Be("Auth.Unauthenticated");
    }

    [Fact]
    public async Task AdminReactivate_ShouldReturnAdminUnauthenticated_WhenActorMissing()
    {
        var security        = Substitute.For<ISecurityService>();
        var auditWriter     = Substitute.For<IAdminAuditWriter>();
        var requestContext  = Substitute.For<IRequestContext>();
        var currentUser     = Substitute.For<ICurrentUser>();

        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var sut = new AdminReactivateUserCommandHandler(
            security, auditWriter, requestContext, currentUser,
            NullLogger<AdminReactivateUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminReactivateUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(AuthErrors.AdminUnauthenticated.Code)
            .And.Be("Auth.Unauthenticated");
    }
}
