using System.Linq.Expressions;
using Accounts.Contracts.Abstractions;
using Auth.Application.Commands.ExternalLogin;
using Auth.Application.ExternalAuth;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Verifies that when secure auto-link refuses, a SINGLE structured Warning
/// log line is emitted naming the exact refusal reason.
///
/// <para>This is the ONLY runtime diagnostic operators have for why a
/// legitimate user got the generic "invalid or expired" response. The client
/// response is intentionally opaque so a real bug or configuration gap
/// surfaces only in the log.</para>
///
/// <para>Each test drives the handler into a single refusal code path and
/// asserts that the warning log text contains "Reason=&lt;enum name&gt;".</para>
/// </summary>
public sealed class ExternalLoginDiagnosticLoggingTests
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

    private readonly CapturingLogger<ExternalLoginCommandHandler> _logger = new();

    private ExternalLoginCommandHandler CreateSut() =>
        new(_extRepo, _deviceRepo, _sessionRepo, _refreshRepo, _uow,
            _security, _registration, _profile, _tokens, _verifier, _nonce, _requestContext, _logger);

    private static ExternalAuthTicket SampleTicket(
        string? email = "user@gmail.com",
        bool emailVerifiedByProvider = true) =>
        new(
            TicketId: Guid.NewGuid(),
            Provider: "google",
            ProviderUserId: "g-1",
            Email: email,
            EmailVerifiedByProvider: emailVerifiedByProvider,
            IssuedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(2));

    private void SetupValidTicketAndNonce(ExternalAuthTicket ticket, bool noLink = true)
    {
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonce.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        if (noLink)
        {
            _extRepo.FindActiveLinkAsync("google", "g-1", Arg.Any<CancellationToken>())
                .Returns((ExternalProvider?)null);
        }
    }

    [Fact]
    public async Task Should_Log_TicketMissingEmail_Reason()
    {
        var ticket = SampleTicket(email: null);
        SetupValidTicketAndNonce(ticket);

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("External login refused", StringComparison.Ordinal)
         && m.Contains("Reason=TicketMissingEmail", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_ProviderDidNotVerifyEmail_Reason()
    {
        var ticket = SampleTicket(emailVerifiedByProvider: false);
        SetupValidTicketAndNonce(ticket);

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("Reason=ProviderDidNotVerifyEmail", StringComparison.Ordinal)
         && m.Contains("ProviderAssertsVerified=False", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_ConcurrentAccountCreated_WhenRegistrationReturnsConflict()
    {
        // The old "NoLocalAccountWithEmail" reason was removed when auto-
        // create was introduced. Now, a missing local account routes to
        // auto-create. If RegisterExternalAsync returns Conflict it means
        // another request just won the race — we fail closed with
        // ConcurrentAccountCreated so nothing is silently merged.
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        _registration.RegisterExternalAsync(
                Arg.Any<ExternalUserRegistrationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Conflict(
                Error.Conflict("User.Email", "An account with this email already exists.")));

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("Reason=ConcurrentAccountCreated", StringComparison.Ordinal)
         && m.Contains("Path=AutoCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_LocalAccountInactive_Reason()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns(userId);
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((SecurityUserData?)null);

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("Reason=LocalAccountInactive", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_LocalEmailNotVerified_Reason()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
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

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("Reason=LocalEmailNotVerified", StringComparison.Ordinal)
         && m.Contains("LocalEmailVerified=False", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_ResolvedEmailMismatch_Reason()
    {
        // Ticket says user@gmail.com but Security resolves a user whose
        // primary email is different (shouldn't happen in a consistent DB
        // but the defensive check exists — prove it trips and logs).
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns(userId);
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId,
                Email: "someoneelse@gmail.com",
                IsEmailVerified: true,
                Roles: Array.Empty<string>(),
                Claims: Array.Empty<(string, string)>()));

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("Reason=ResolvedEmailMismatch", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_UserAlreadyHasDifferentLinkOnSameProvider_Reason()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns(userId);
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId, Email: "user@gmail.com", IsEmailVerified: true,
                Roles: Array.Empty<string>(), Claims: Array.Empty<(string, string)>()));
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("Reason=UserAlreadyHasDifferentLinkOnSameProvider", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_ProviderIdentityOwnedByAnotherUser_Reason()
    {
        var ticket = SampleTicket();
        SetupValidTicketAndNonce(ticket);
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync("user@gmail.com", Arg.Any<CancellationToken>())
            .Returns(userId);
        _security.GetUserDataByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new SecurityUserData(
                UserId: userId, Email: "user@gmail.com", IsEmailVerified: true,
                Roles: Array.Empty<string>(), Claims: Array.Empty<(string, string)>()));
        _extRepo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false, true);

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.WarningMessages.Should().Contain(m =>
            m.Contains("Reason=ProviderIdentityOwnedByAnotherUser", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_Log_AttemptSummary_AtInformation_ForEveryAttempt()
    {
        // Every attempt — including refusals — should emit a single
        // "External login attempt:" summary log line so operators can trace
        // the full round trip.
        var ticket = SampleTicket(email: null);
        SetupValidTicketAndNonce(ticket);

        await CreateSut().Handle(
            new ExternalLoginCommand("t", "test-recaptcha-token"),
            CancellationToken.None);

        _logger.InformationMessages.Should().Contain(m =>
            m.Contains("External login attempt:", StringComparison.Ordinal)
         && m.Contains("Provider=google", StringComparison.Ordinal)
         && m.Contains("ProviderUserId=g-1", StringComparison.Ordinal));
    }

    /// <summary>
    /// In-memory <see cref="ILogger{TCategory}"/> that snapshots the formatted
    /// message for every Warning and Information log call.
    /// </summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> WarningMessages { get; } = new();
        public List<string> InformationMessages { get; } = new();

#pragma warning disable SA1127 // Generic type constraints should be on their own line
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
#pragma warning restore SA1127 // Generic type constraints should be on their own line

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            if (logLevel == LogLevel.Warning) WarningMessages.Add(msg);
            if (logLevel == LogLevel.Information) InformationMessages.Add(msg);
        }
    }
}
