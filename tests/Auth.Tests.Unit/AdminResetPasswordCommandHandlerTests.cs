using Auth.Application.Commands.AdminResetPassword;
using Auth.Application.Interfaces;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 3A — covers <see cref="AdminResetPasswordCommandHandler"/>.
/// The handler reuses the Phase 2C-2 <see cref="PasswordResetToken"/>
/// aggregate and Phase 2C-3 outbox pipeline; these tests verify:
///   • auth guard (unauthenticated actor),
///   • cross-module eligibility propagation (NotFound / Forbidden / self),
///   • lifecycle gate (rejects Provisioned / PendingActivation / Suspended / Archived),
///   • primary-email-verified gate,
///   • happy path (Active) — token issued with AdminInitiated origin,
///     domain event attached, prior tokens superseded, lifecycle
///     transitioned, sessions revoked with PasswordResetByAdmin,
///     no IEmailService call, single UoW save,
///   • idempotent re-issue from PendingPasswordReset,
///   • plain code is NEVER logged.
/// </summary>
public sealed class AdminResetPasswordCommandHandlerTests
{
    private const string AdminEmail = "admin@example.com";

    private readonly ISecurityService              _security          = Substitute.For<ISecurityService>();
    private readonly IUserRegistrationService      _registration      = Substitute.For<IUserRegistrationService>();
    private readonly IPasswordResetTokenRepository _tokenRepo         = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IAuthUnitOfWork               _uow               = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService                   _otpService        = Substitute.For<IOtpService>();
    private readonly ISessionRevocationService     _sessionRevocation = Substitute.For<ISessionRevocationService>();
    private readonly IAdminAuditWriter             _auditWriter       = Substitute.For<IAdminAuditWriter>();
    private readonly IRequestContext               _requestContext    = Substitute.For<IRequestContext>();
    private readonly ICurrentUser                  _currentUser       = Substitute.For<ICurrentUser>();
    private readonly HybridCache                   _cache             = Substitute.For<HybridCache>();
    private readonly Guid                          _actorId           = Guid.NewGuid();

    public AdminResetPasswordCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_actorId);
        _currentUser.Email.Returns(AdminEmail);

        _sessionRevocation
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));
    }

    private AdminResetPasswordCommandHandler CreateSut(ILogger<AdminResetPasswordCommandHandler>? logger = null) =>
        new(_security, _registration, _tokenRepo, _uow, _otpService, _sessionRevocation,
            _auditWriter, _requestContext, _currentUser, _cache,
            logger ?? NullLogger<AdminResetPasswordCommandHandler>.Instance);

    private static AdminResetPasswordCommand Command(Guid targetId, string? reason = "left company") =>
        new(targetId, reason);

    private void StubEligibility(
        Guid targetId,
        AccountLifecycleSnapshot lifecycle,
        bool isPrimaryEmailVerified = true,
        string primaryEmail = "target@example.com")
    {
        _security.GetAdminResetEligibilityAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminResetEligibility>.Success(new AdminResetEligibility(
                TargetUserId:           targetId,
                PrimaryEmail:           primaryEmail,
                IsPrimaryEmailVerified: isPrimaryEmailVerified,
                Lifecycle:              lifecycle)));
    }

    private void StubNoActiveTokens() =>
        _tokenRepo.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PasswordResetToken>());

    private void StubOtp() =>
        _otpService.Generate().Returns("909090").AndDoes(_ => { });

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenActorNotAuthenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);

        await _security.DidNotReceive()
            .GetAdminResetEligibilityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        // Phase 4 — failures must NOT write audit rows.
        await _auditWriter.DidNotReceive()
            .RecordAsync(Arg.Any<AdminAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        var targetId = Guid.NewGuid();
        _security.GetAdminResetEligibilityAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminResetEligibility>.Failure(
                Error.NotFound("User.NotFound", "No account found."), Outcome.NotFound));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenHierarchyDeniesActor()
    {
        // Also covers self-management denial — the hierarchy service
        // already rejects privileged self-management (verified in
        // RoleHierarchyServiceTests). The handler simply propagates.
        var targetId = Guid.NewGuid();
        _security.GetAdminResetEligibilityAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminResetEligibility>.Failure(
                Error.Forbidden("You do not have permission to manage this user."),
                Outcome.Forbidden));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);

        await _registration.DidNotReceive()
            .MarkPendingPasswordResetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AccountLifecycleSnapshot.Provisioned)]
    [InlineData(AccountLifecycleSnapshot.PendingActivation)]
    [InlineData(AccountLifecycleSnapshot.Suspended)]
    [InlineData(AccountLifecycleSnapshot.Archived)]
    public async Task Handle_ShouldReturnConflict_WhenLifecycleIneligible(AccountLifecycleSnapshot lifecycle)
    {
        var targetId = Guid.NewGuid();
        StubEligibility(targetId, lifecycle);

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _registration.DidNotReceive()
            .MarkPendingPasswordResetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenPrimaryEmailUnverified()
    {
        var targetId = Guid.NewGuid();
        StubEligibility(targetId, AccountLifecycleSnapshot.Active, isPrimaryEmailVerified: false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnHappyPath_ShouldIssueAdminInitiatedToken_TransitionLifecycle_AndRevokeSessions()
    {
        var targetId = Guid.NewGuid();
        StubEligibility(targetId, AccountLifecycleSnapshot.Active, primaryEmail: "user@example.com");
        StubNoActiveTokens();
        StubOtp();
        _otpService.Hash(Arg.Any<string>()).Returns("hash-xyz");

        _registration.MarkPendingPasswordResetAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        PasswordResetToken? persisted = null;
        _tokenRepo.AddAsync(Arg.Do<PasswordResetToken>(t => persisted = t), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId, reason: "Suspected compromise"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().NotBeNullOrWhiteSpace();

        // Token persisted in Issued/Pending with AdminInitiated origin.
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(targetId);
        persisted.TokenHash.Should().Be("hash-xyz");
        persisted.DeliveryAddress.Should().Be("user@example.com");
        persisted.ResetOrigin.Should().Be(PasswordResetOrigin.AdminInitiated);
        persisted.State.Should().Be(PasswordResetTokenState.Issued);
        persisted.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Pending);

        // Domain event attached so the outbox dispatch pipeline picks it up.
        var domainEvent = persisted.DomainEvents.OfType<PasswordResetTokenIssuedEvent>().Single();
        domainEvent.TokenId.Should().Be(persisted.Id);
        domainEvent.UserId.Should().Be(targetId);
        domainEvent.PlainCode.Should().Be("909090");
        domainEvent.Origin.Should().Be(PasswordResetOrigin.AdminInitiated);

        // Lifecycle transition invoked.
        await _registration.Received(1)
            .MarkPendingPasswordResetAsync(targetId, Arg.Any<CancellationToken>());

        // Session revocation invoked with the 3A-specific reason.
        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(
                targetId,
                SessionRevocationReason.PasswordResetByAdmin,
                Arg.Any<CancellationToken>());

        // Single Auth UoW save committed everything.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // Phase 4 — admin audit row appended on success path. Action
        // verb matches the constant; reason is forwarded; metadata
        // includes the issued token id but NEVER the plain code.
        await _auditWriter.Received(1).RecordAsync(
            Arg.Is<AdminAuditEntry>(e =>
                e.Action       == AuditActions.AdminResetPasswordInitiated
             && e.ActorUserId  == _actorId
             && e.TargetUserId == targetId
             && e.Reason       == "Suspected compromise"
             && e.Metadata     != null
             && e.Metadata.Contains(persisted!.Id.ToString("D"), StringComparison.Ordinal)
             && !e.Metadata.Contains("909090", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSupersedePriorActiveTokens_BeforeIssuingNewOne()
    {
        var targetId = Guid.NewGuid();
        StubEligibility(targetId, AccountLifecycleSnapshot.Active);

        var old1 = PasswordResetToken.Issue(targetId, "h1", "user@example.com", 10);
        var old2 = PasswordResetToken.Issue(targetId, "h2", "user@example.com", 10);
        old2.MarkDelivered();

        _tokenRepo.GetActiveForUserAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new[] { old1, old2 });

        StubOtp();
        _otpService.Hash(Arg.Any<string>()).Returns("h-new");

        _registration.MarkPendingPasswordResetAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        old1.State.Should().Be(PasswordResetTokenState.Revoked);
        old1.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
        old2.State.Should().Be(PasswordResetTokenState.Revoked);
        old2.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenAccountIsAlreadyPendingPasswordReset()
    {
        // Re-issuing an admin reset for a user already in PendingPasswordReset
        // is legal: a fresh token is issued, the old one is superseded,
        // MarkPendingPasswordResetAsync returns Success as a no-op.
        var targetId = Guid.NewGuid();
        StubEligibility(targetId, AccountLifecycleSnapshot.PendingPasswordReset);
        StubNoActiveTokens();
        StubOtp();
        _otpService.Hash(Arg.Any<string>()).Returns("hash");

        _registration.MarkPendingPasswordResetAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _registration.Received(1)
            .MarkPendingPasswordResetAsync(targetId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNeverCallEmailService_Directly()
    {
        // Contract invariant: admin reset goes through the outbox
        // pipeline. The handler has no IEmailService dependency at all,
        // so this test is proved by construction — but we assert it via
        // the constructor's dependency list to lock the invariant.
        var sut = CreateSut();
        var deps = sut.GetType().GetConstructors()
            .Single()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();

        deps.Should().NotContain(typeof(IEmailService),
            "AdminResetPasswordCommandHandler must not take IEmailService — email is dispatched asynchronously via the outbox.");
    }

    [Fact]
    public async Task Handle_ShouldNeverLog_PlainCode()
    {
        // Capture log entries and assert the plain code never appears
        // in any message or argument.
        var plainCode = "123456";
        var spy = new LogSpy<AdminResetPasswordCommandHandler>();

        var targetId = Guid.NewGuid();
        StubEligibility(targetId, AccountLifecycleSnapshot.Active);
        StubNoActiveTokens();
        _otpService.Generate().Returns(plainCode);
        _otpService.Hash(Arg.Any<string>()).Returns("hash");
        _registration.MarkPendingPasswordResetAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = CreateSut(spy);

        await sut.Handle(Command(targetId, reason: "routine"), CancellationToken.None);

        spy.AssertDoesNotContain(plainCode,
            "the plain reset code must never travel through structured logs");
    }

    // ── Test-only log spy ─────────────────────────────────────────────────────

    private sealed class LogSpy<TCategory> : ILogger<TCategory>
    {
        private readonly List<string> _messages = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
            // Also walk the state if it's a key-value pair list — catches
            // cases where a value-parameter might have leaked a code.
            if (state is IReadOnlyList<KeyValuePair<string, object?>> kvps)
            {
                foreach (var kv in kvps)
                {
                    if (kv.Value is string s) _messages.Add(s);
                }
            }
        }

        public void AssertDoesNotContain(string forbidden, string because)
        {
            _messages.Should().NotContain(m => m.Contains(forbidden, StringComparison.Ordinal), because);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
