using Auth.Application.Commands.AdminArchiveUser;
using Auth.Application.Commands.AdminReactivateUser;
using Auth.Application.Commands.AdminSuspendUser;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

public sealed class AdminUserLifecycleCommandHandlerTests
{
    private readonly ISecurityService _securityService = Substitute.For<ISecurityService>();
    private readonly ISessionRevocationService _sessionRevocation = Substitute.For<ISessionRevocationService>();
    private readonly IAuthUnitOfWork _authUnitOfWork = Substitute.For<IAuthUnitOfWork>();
    private readonly IAdminAuditWriter _auditWriter = Substitute.For<IAdminAuditWriter>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _actorId = Guid.NewGuid();

    public AdminUserLifecycleCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_actorId);

        _sessionRevocation
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));
    }

    [Fact]
    public async Task Suspend_ShouldReturnUnauthorized_WhenActorNotAuthenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = new AdminSuspendUserCommandHandler(
            _securityService,
            _sessionRevocation,
            _authUnitOfWork,
            _auditWriter,
            _requestContext,
            _currentUser,
            NullLogger<AdminSuspendUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminSuspendUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);

        await _securityService.DidNotReceive()
            .SuspendUserByAdminAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        // Phase 4 — failures must NOT write audit rows.
        await _auditWriter.DidNotReceive()
            .RecordAsync(Arg.Any<AdminAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Suspend_ShouldTransition_RevokeSessions_AndSave()
    {
        var targetUserId = Guid.NewGuid();

        _securityService.SuspendUserByAdminAsync(targetUserId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new AdminSuspendUserCommandHandler(
            _securityService,
            _sessionRevocation,
            _authUnitOfWork,
            _auditWriter,
            _requestContext,
            _currentUser,
            NullLogger<AdminSuspendUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminSuspendUserCommand(targetUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _securityService.Received(1)
            .SuspendUserByAdminAsync(targetUserId, _actorId, Arg.Any<CancellationToken>());

        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(targetUserId, SessionRevocationReason.AccountSuspended, Arg.Any<CancellationToken>());

        await _authUnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // Phase 4 — admin audit row is appended on the success path.
        await _auditWriter.Received(1).RecordAsync(
            Arg.Is<AdminAuditEntry>(e =>
                e.ActorUserId  == _actorId
             && e.TargetUserId == targetUserId
             && e.Action       == AuditActions.AdminSuspendUser),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Suspend_ShouldPropagateFailure_WithoutRevocation()
    {
        var targetUserId = Guid.NewGuid();

        _securityService.SuspendUserByAdminAsync(targetUserId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Conflict("User.IneligibleForSuspend", "ineligible"), Outcome.Conflict));

        var sut = new AdminSuspendUserCommandHandler(
            _securityService,
            _sessionRevocation,
            _authUnitOfWork,
            _auditWriter,
            _requestContext,
            _currentUser,
            NullLogger<AdminSuspendUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminSuspendUserCommand(targetUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
        await _authUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

        // Phase 4 — failures must NOT write audit rows.
        await _auditWriter.DidNotReceive()
            .RecordAsync(Arg.Any<AdminAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reactivate_ShouldTransition_WithoutSessionRevocation()
    {
        var targetUserId = Guid.NewGuid();

        _securityService.ReactivateUserByAdminAsync(targetUserId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new AdminReactivateUserCommandHandler(
            _securityService,
            _auditWriter,
            _requestContext,
            _currentUser,
            NullLogger<AdminReactivateUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminReactivateUserCommand(targetUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _securityService.Received(1)
            .ReactivateUserByAdminAsync(targetUserId, _actorId, Arg.Any<CancellationToken>());

        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());

        await _authUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

        // Phase 4 — admin audit row is appended on the success path.
        await _auditWriter.Received(1).RecordAsync(
            Arg.Is<AdminAuditEntry>(e =>
                e.ActorUserId  == _actorId
             && e.TargetUserId == targetUserId
             && e.Action       == AuditActions.AdminReactivateUser),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_ShouldTransition_RevokeSessions_AndSave()
    {
        var targetUserId = Guid.NewGuid();

        _securityService.ArchiveUserByAdminAsync(targetUserId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new AdminArchiveUserCommandHandler(
            _securityService,
            _sessionRevocation,
            _authUnitOfWork,
            _auditWriter,
            _requestContext,
            _currentUser,
            NullLogger<AdminArchiveUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminArchiveUserCommand(targetUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _securityService.Received(1)
            .ArchiveUserByAdminAsync(targetUserId, _actorId, Arg.Any<CancellationToken>());

        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(targetUserId, SessionRevocationReason.AccountArchived, Arg.Any<CancellationToken>());

        await _authUnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // Phase 4 — admin audit row is appended on the success path.
        await _auditWriter.Received(1).RecordAsync(
            Arg.Is<AdminAuditEntry>(e =>
                e.ActorUserId  == _actorId
             && e.TargetUserId == targetUserId
             && e.Action       == AuditActions.AdminArchiveUser),
            Arg.Any<CancellationToken>());
    }
}
