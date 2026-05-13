using Accounts.Contracts.Abstractions;
using Auth.Application.Commands.AdminReassignAccount;
using Auth.Application.Interfaces;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Entities;
using Auth.Domain.Events;
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
/// Phase 3C — covers <see cref="AdminReassignAccountCommandHandler"/>.
/// Verifies:
///   • Authentication guard (unauthenticated actor),
///   • Propagation of Security-side failures (NotFound / Forbidden / Conflict),
///   • Lockout semantics: sessions revoked with AccountReassigned,
///     activation + reset tokens superseded, external provider links deactivated,
///     new activation token issued with ActivationTokenIssuedEvent,
///   • Single Auth UoW save,
///   • Plain activation token never logged.
/// </summary>
public sealed class AdminReassignAccountCommandHandlerTests
{
    private readonly ISecurityService              _security                    = Substitute.For<ISecurityService>();
    private readonly IActivationTokenRepository    _activationTokens            = Substitute.For<IActivationTokenRepository>();
    private readonly IPasswordResetTokenRepository _resetTokens                 = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IExternalProviderRepository   _externalProviders           = Substitute.For<IExternalProviderRepository>();
    private readonly ISessionRevocationService     _sessionRevocation           = Substitute.For<ISessionRevocationService>();
    private readonly IAuthUnitOfWork               _uow                         = Substitute.For<IAuthUnitOfWork>();
    private readonly PassThroughTransactionalExecutor _txExecutor               = new();
    private readonly IInviteTokenService           _inviteTokenService          = Substitute.For<IInviteTokenService>();
    private readonly IInviteLinkBuilder            _inviteLinkBuilder           = Substitute.For<IInviteLinkBuilder>();
    private readonly IProfileReassignmentService   _profileReassignment         = Substitute.For<IProfileReassignmentService>();
    private readonly IAdminAuditWriter             _auditWriter                 = Substitute.For<IAdminAuditWriter>();
    private readonly IRequestContext               _requestContext              = Substitute.For<IRequestContext>();
    private readonly ICurrentUser                  _currentUser                 = Substitute.For<ICurrentUser>();
    private readonly HybridCache                   _cache                       = Substitute.For<HybridCache>();
    private readonly Guid                          _actorId                     = Guid.NewGuid();

    public AdminReassignAccountCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_actorId);

        _sessionRevocation
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));

        _inviteTokenService.Generate().Returns("PLAIN-TOKEN");
        _inviteTokenService.Hash(Arg.Any<string>()).Returns("hash-xyz");
        _inviteLinkBuilder.Build(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => $"https://app.example.com/activate?email={ci.ArgAt<string>(0)}&token={ci.ArgAt<string>(1)}");

        // Phase 3D / 4 — profile reassignment defaults to Success
        // with Scrubbed=true; individual tests override when a
        // missing-profile or failure path is under test.
        _profileReassignment
            .ResetForReassignmentAsync(Arg.Any<ProfileReassignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: true)));
    }

    private AdminReassignAccountCommandHandler CreateSut() =>
        new(_security, _activationTokens, _resetTokens, _externalProviders,
            _sessionRevocation, _uow, _txExecutor, _inviteTokenService, _inviteLinkBuilder,
            _profileReassignment, _auditWriter, _requestContext, _currentUser, _cache,
            NullLogger<AdminReassignAccountCommandHandler>.Instance);

    private static AdminReassignAccountCommand Command(
        Guid targetId,
        string newEmail = "new@example.com",
        string? reason = "reassignment test") =>
        new(targetId, newEmail, reason);

    private void StubSecuritySuccess(
        Guid targetId,
        string oldEmail = "old@example.com",
        string newEmail = "new@example.com")
    {
        _security.ReassignUserByAdminAsync(
                targetId, _actorId, newEmail, Arg.Any<CancellationToken>())
            .Returns(Result<ReassignmentCompleted>.Success(new ReassignmentCompleted(
                TargetUserId: targetId,
                OldEmail:     oldEmail,
                NewEmail:     newEmail,
                Lifecycle:    AccountLifecycleSnapshot.PendingActivation)));
    }

    private void StubNoExistingActivationTokens() =>
        _activationTokens.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActivationToken>());

    private void StubNoExistingResetTokens() =>
        _resetTokens.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PasswordResetToken>());

    private void StubNoActiveProviderLinks() =>
        _externalProviders.GetAllAsync(
                filter:      Arg.Any<System.Linq.Expressions.Expression<Func<ExternalProvider, bool>>>(),
                include:     Arg.Any<Func<IQueryable<ExternalProvider>, IQueryable<ExternalProvider>>?>(),
                orderBy:     Arg.Any<Func<IQueryable<ExternalProvider>, IOrderedQueryable<ExternalProvider>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:          Arg.Any<CancellationToken>())
            .Returns(new List<ExternalProvider>());

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
            .ReassignUserByAdminAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _txExecutor.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSecurityReportsUserMissing()
    {
        var targetId = Guid.NewGuid();
        _security.ReassignUserByAdminAsync(targetId, _actorId, "new@example.com", Arg.Any<CancellationToken>())
            .Returns(Result<ReassignmentCompleted>.Failure(
                Error.NotFound("User.NotFound", "No account found."), Outcome.NotFound));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);

        // No downstream mutations may occur on a Security failure.
        await _activationTokens.DidNotReceive().AddAsync(Arg.Any<ActivationToken>(), Arg.Any<CancellationToken>());
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenHierarchyDenies()
    {
        var targetId = Guid.NewGuid();
        _security.ReassignUserByAdminAsync(targetId, _actorId, "new@example.com", Arg.Any<CancellationToken>())
            .Returns(Result<ReassignmentCompleted>.Failure(
                Error.Forbidden("You do not have permission to manage this user."),
                Outcome.Forbidden));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);

        await _activationTokens.DidNotReceive().AddAsync(Arg.Any<ActivationToken>(), Arg.Any<CancellationToken>());
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenLifecycleIneligible()
    {
        var targetId = Guid.NewGuid();
        _security.ReassignUserByAdminAsync(targetId, _actorId, "new@example.com", Arg.Any<CancellationToken>())
            .Returns(Result<ReassignmentCompleted>.Failure(
                Error.Conflict("User.IneligibleForReassign", "ineligible"),
                Outcome.Conflict));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        await _activationTokens.DidNotReceive().AddAsync(Arg.Any<ActivationToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenEmailAlreadyInUse()
    {
        var targetId = Guid.NewGuid();
        _security.ReassignUserByAdminAsync(targetId, _actorId, "taken@example.com", Arg.Any<CancellationToken>())
            .Returns(Result<ReassignmentCompleted>.Failure(
                Error.Conflict("User.Email", "An account with this email already exists."),
                Outcome.Conflict));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId, newEmail: "taken@example.com"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        await _activationTokens.DidNotReceive().AddAsync(Arg.Any<ActivationToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnHappyPath_ShouldIssueActivationToken_RevokeSessions_AndInvalidateAllCredentialVectors()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId, newEmail: "new@example.com");
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        ActivationToken? persisted = null;
        _activationTokens.AddAsync(Arg.Do<ActivationToken>(t => persisted = t), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId, newEmail: "new@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().NotBeNullOrWhiteSpace();

        // Activation token issued for the NEW email, with Issued/Pending state.
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(targetId);
        persisted.TokenHash.Should().Be("hash-xyz");
        persisted.DeliveryAddress.Should().Be("new@example.com");
        persisted.State.Should().Be(ActivationTokenState.Issued);
        persisted.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Pending);

        // Domain event attached so outbox dispatch picks it up.
        var domainEvent = persisted.DomainEvents.OfType<ActivationTokenIssuedEvent>().Single();
        domainEvent.TokenId.Should().Be(persisted.Id);
        domainEvent.UserId.Should().Be(targetId);
        domainEvent.PlainToken.Should().Be("PLAIN-TOKEN");
        domainEvent.DeliveryAddress.Should().Be("new@example.com");

        // Sessions + refresh tokens revoked with AccountReassigned.
        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(
                targetId,
                SessionRevocationReason.AccountReassigned,
                Arg.Any<CancellationToken>());

        // Executor invoked once (single cross-module transactional unit).
        _txExecutor.InvocationCount.Should().Be(1);

        // Single Auth UoW SaveChanges — commits token + supersedes +
        // provider deactivations + session revocations atomically.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSupersedeAllActiveActivationTokens()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        var old1 = ActivationToken.Issue(targetId, "h1", "old@example.com", 60);
        var old2 = ActivationToken.Issue(targetId, "h2", "old@example.com", 60);
        old2.MarkDelivered();

        _activationTokens.GetActiveForUserAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new[] { old1, old2 });

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        old1.State.Should().Be(ActivationTokenState.Revoked);
        old1.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
        old2.State.Should().Be(ActivationTokenState.Revoked);
        old2.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
    }

    [Fact]
    public async Task Handle_ShouldSupersedeAllActivePasswordResetTokens()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingActivationTokens();
        StubNoActiveProviderLinks();

        var old1 = PasswordResetToken.Issue(targetId, "h1", "old@example.com", 10);
        var old2 = PasswordResetToken.Issue(targetId, "h2", "old@example.com", 10);
        old2.MarkDelivered();

        _resetTokens.GetActiveForUserAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(new[] { old1, old2 });

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        old1.State.Should().Be(PasswordResetTokenState.Revoked);
        old1.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
        old2.State.Should().Be(PasswordResetTokenState.Revoked);
        old2.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
    }

    [Fact]
    public async Task Handle_ShouldDeactivateAllActiveExternalProviderLinks()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();

        var google = ExternalProvider.Create(targetId, "google", "g-1", "old@example.com");
        var facebook = ExternalProvider.Create(targetId, "facebook", "f-1", "old@example.com");

        _externalProviders.GetAllAsync(
                filter:      Arg.Any<System.Linq.Expressions.Expression<Func<ExternalProvider, bool>>>(),
                include:     Arg.Any<Func<IQueryable<ExternalProvider>, IQueryable<ExternalProvider>>?>(),
                orderBy:     Arg.Any<Func<IQueryable<ExternalProvider>, IOrderedQueryable<ExternalProvider>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:          Arg.Any<CancellationToken>())
            .Returns(new List<ExternalProvider> { google, facebook });

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        google.IsActive.Should().BeFalse("the old owner must not retain Google sign-in");
        facebook.IsActive.Should().BeFalse("the old owner must not retain Facebook sign-in");
    }

    [Fact]
    public async Task Handle_OnHappyPath_ShouldCallProfileReassignmentService_WithNewEmail()
    {
        // Phase 3D — the handler must scrub the Accounts profile as
        // part of the same transactional unit as the Security/Auth
        // reassignment.
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId, newEmail: "new@example.com");
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        ProfileReassignmentRequest? captured = null;
        _profileReassignment
            .ResetForReassignmentAsync(
                Arg.Do<ProfileReassignmentRequest>(r => captured = r),
                Arg.Any<CancellationToken>())
            .Returns(Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: true)));

        var sut = CreateSut();

        var result = await sut.Handle(
            Command(targetId, newEmail: "new@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _profileReassignment.Received(1).ResetForReassignmentAsync(
            Arg.Any<ProfileReassignmentRequest>(),
            Arg.Any<CancellationToken>());

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(targetId,
            "the reassigned UserId must be forwarded to Accounts");
        captured.NewEmail.Should().Be("new@example.com",
            "Accounts derives the placeholder DisplayName from the new email local-part");
    }

    [Fact]
    public async Task Handle_WhenProfileReassignmentFails_ShouldPropagateFailure()
    {
        // Phase 3D — if the Accounts scrub fails, the reassignment must
        // not silently succeed. Since PassThroughTransactionalExecutor
        // bypasses TransactionScope, we assert the handler returns a
        // failure Result; in production the ambient TransactionScope
        // would also dispose without Complete() and roll Security +
        // Auth back.
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        _profileReassignment
            .ResetForReassignmentAsync(
                Arg.Any<ProfileReassignmentRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<ProfileReassignmentOutcome>.Failure(
                Error.Failure("Profile.Reset", "profile reset failed"),
                Outcome.ServerError));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue(
            "a failing profile scrub must propagate — no silent success");
        result.Outcome.Should().Be(Outcome.ServerError);

        // The Auth UoW SaveChanges must NOT have been called after a
        // profile-reset failure (the handler returns before the flush).
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNeverLog_PlainActivationToken()
    {
        var spy = new LogSpy<AdminReassignAccountCommandHandler>();

        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        _inviteTokenService.Generate().Returns("SECRET-PLAIN-TOKEN-123");

        var sut = new AdminReassignAccountCommandHandler(
            _security, _activationTokens, _resetTokens, _externalProviders,
            _sessionRevocation, _uow, _txExecutor, _inviteTokenService, _inviteLinkBuilder,
            _profileReassignment, _auditWriter, _requestContext, _currentUser, _cache, spy);

        await sut.Handle(Command(targetId), CancellationToken.None);

        spy.AssertDoesNotContain("SECRET-PLAIN-TOKEN-123",
            "the plain activation token must never travel through structured logs");
    }

    // ── Phase 4 — admin audit timeline ───────────────────────────────────────

    [Fact]
    public async Task Handle_OnSuccess_ShouldRecord_Exactly_OneAuditEntry_WithReassignAction()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId, oldEmail: "old@example.com", newEmail: "new@example.com");
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        _requestContext.IpAddress.Returns("203.0.113.42");

        var sut = CreateSut();

        await sut.Handle(
            Command(targetId, newEmail: "new@example.com", reason: "left company"),
            CancellationToken.None);

        await _auditWriter.Received(1).RecordAsync(
            Arg.Is<AdminAuditEntry>(e =>
                e.Action       == AuditActions.AdminReassignAccount
             && e.ActorUserId  == _actorId
             && e.TargetUserId == targetId
             && e.Reason       == "left company"
             && e.IpAddress    == "203.0.113.42"),
            Arg.Any<CancellationToken>());

        // Per Option C — exactly ONE audit row is emitted; the
        // profile-scrub fact lives inside Metadata, not as a second
        // row.
        await _auditWriter.Received(1).RecordAsync(
            Arg.Any<AdminAuditEntry>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccessWithScrubbedProfile_ShouldEmbed_ProfileScrubbedTrue_InMetadata()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId, oldEmail: "old@example.com", newEmail: "new@example.com");
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        _profileReassignment
            .ResetForReassignmentAsync(Arg.Any<ProfileReassignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: true)));

        AdminAuditEntry? captured = null;
        _auditWriter
            .RecordAsync(Arg.Do<AdminAuditEntry>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        await sut.Handle(Command(targetId), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Metadata.Should().NotBeNull();
        captured.Metadata!.Should().Contain("\"profileScrubbed\":true");
        captured.Metadata.Should().Contain("\"oldEmail\":\"old@example.com\"");
        captured.Metadata.Should().Contain("\"newEmail\":\"new@example.com\"");
    }

    [Fact]
    public async Task Handle_OnSuccessWithMissingProfile_ShouldEmbed_ProfileScrubbedFalse_InMetadata()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        _profileReassignment
            .ResetForReassignmentAsync(Arg.Any<ProfileReassignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: false)));

        AdminAuditEntry? captured = null;
        _auditWriter
            .RecordAsync(Arg.Do<AdminAuditEntry>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        await sut.Handle(Command(targetId), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Metadata.Should().Contain("\"profileScrubbed\":false",
            "Option C — missing-profile no-op must be reflected truthfully in metadata, " +
            "not by the absence of a separate audit row.");
    }

    [Fact]
    public async Task Handle_OnSecurityFailure_ShouldNotRecordAudit()
    {
        var targetId = Guid.NewGuid();
        _security.ReassignUserByAdminAsync(
                targetId, _actorId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReassignmentCompleted>.Failure(
                Error.Conflict("User.IneligibleForReassign", "ineligible"), Outcome.Conflict));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        await _auditWriter.DidNotReceive()
            .RecordAsync(Arg.Any<AdminAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnProfileFailure_ShouldNotRecordAudit()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        _profileReassignment
            .ResetForReassignmentAsync(Arg.Any<ProfileReassignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProfileReassignmentOutcome>.Failure(
                Error.Failure("Profile.Reset", "boom"), Outcome.ServerError));

        var sut = CreateSut();

        var result = await sut.Handle(Command(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        await _auditWriter.DidNotReceive()
            .RecordAsync(Arg.Any<AdminAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditMetadata_ShouldNeverContain_PlainActivationToken()
    {
        var targetId = Guid.NewGuid();
        StubSecuritySuccess(targetId);
        StubNoExistingActivationTokens();
        StubNoExistingResetTokens();
        StubNoActiveProviderLinks();

        _inviteTokenService.Generate().Returns("SECRET-PLAIN-TOKEN-XYZ");

        AdminAuditEntry? captured = null;
        _auditWriter
            .RecordAsync(Arg.Do<AdminAuditEntry>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        await sut.Handle(Command(targetId), CancellationToken.None);

        captured.Should().NotBeNull();
        (captured!.Metadata ?? string.Empty).Should().NotContain("SECRET-PLAIN-TOKEN-XYZ",
            "the plain activation token must never travel through the audit metadata");
    }

    // ── Test-only log spy ─────────────────────────────────────────────────────

    private sealed class LogSpy<TCategory> : Microsoft.Extensions.Logging.ILogger<TCategory>
    {
        private readonly List<string> _messages = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
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
