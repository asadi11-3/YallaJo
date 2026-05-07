using Auth.Application.Caching;
using Auth.Application.Commands.AdminArchiveUser;
using Auth.Application.Commands.AdminSuspendUser;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Repositories;
using Auth.Infrastructure.EventHandlers;
using Auth.Application.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Security.Contracts.Abstractions;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 1 (AUTH-STD-3) — guarantees that handlers which revoke sessions via
/// <see cref="ISessionRevocationService"/> invalidate the per-user
/// active-sessions cache (<see cref="AuthCacheKeys.UserSessionsTag"/>) only
/// AFTER successful <c>SaveChangesAsync</c>. If persistence fails, the cache
/// must NOT be invalidated.
/// </summary>
public sealed class SessionRevocationCacheInvalidationTests
{
    private readonly ISecurityService _security = Substitute.For<ISecurityService>();
    private readonly ISessionRevocationService _sessionRevocation = Substitute.For<ISessionRevocationService>();
    private readonly IAuthUnitOfWork _uow = Substitute.For<IAuthUnitOfWork>();
    private readonly IAdminAuditWriter _auditWriter = Substitute.For<IAdminAuditWriter>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly Guid _actorId = Guid.NewGuid();

    public SessionRevocationCacheInvalidationTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_actorId);

        _sessionRevocation
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));
    }

    // ── AdminSuspendUser ──────────────────────────────────────────────────────

    [Fact]
    public async Task AdminSuspend_ShouldInvalidateSessionsCache_AfterSaveChanges()
    {
        var targetId = Guid.NewGuid();
        _security.SuspendUserByAdminAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new AdminSuspendUserCommandHandler(
            _security, _sessionRevocation, _uow, _auditWriter,
            _requestContext, _currentUser, _cache,
            NullLogger<AdminSuspendUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminSuspendUserCommand(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(targetId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminSuspend_ShouldNotInvalidateCache_WhenSaveChangesFails()
    {
        var targetId = Guid.NewGuid();
        _security.SuspendUserByAdminAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB failed"));

        var sut = new AdminSuspendUserCommandHandler(
            _security, _sessionRevocation, _uow, _auditWriter,
            _requestContext, _currentUser, _cache,
            NullLogger<AdminSuspendUserCommandHandler>.Instance);

        Func<Task> act = () => sut.Handle(new AdminSuspendUserCommand(targetId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Cache invalidation MUST NOT happen if persistence didn't succeed.
        await _cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminSuspend_ShouldNotInvalidateCache_WhenSecurityServiceFails()
    {
        var targetId = Guid.NewGuid();
        _security.SuspendUserByAdminAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Conflict("User.Ineligible", "ineligible"), Outcome.Conflict));

        var sut = new AdminSuspendUserCommandHandler(
            _security, _sessionRevocation, _uow, _auditWriter,
            _requestContext, _currentUser, _cache,
            NullLogger<AdminSuspendUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminSuspendUserCommand(targetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── AdminArchiveUser ──────────────────────────────────────────────────────

    [Fact]
    public async Task AdminArchive_ShouldInvalidateSessionsCache_AfterSaveChanges()
    {
        var targetId = Guid.NewGuid();
        _security.ArchiveUserByAdminAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new AdminArchiveUserCommandHandler(
            _security, _sessionRevocation, _uow, _auditWriter,
            _requestContext, _currentUser, _cache,
            NullLogger<AdminArchiveUserCommandHandler>.Instance);

        var result = await sut.Handle(new AdminArchiveUserCommand(targetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(targetId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminArchive_ShouldNotInvalidateCache_WhenSaveChangesFails()
    {
        var targetId = Guid.NewGuid();
        _security.ArchiveUserByAdminAsync(targetId, _actorId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB failed"));

        var sut = new AdminArchiveUserCommandHandler(
            _security, _sessionRevocation, _uow, _auditWriter,
            _requestContext, _currentUser, _cache,
            NullLogger<AdminArchiveUserCommandHandler>.Instance);

        Func<Task> act = () => sut.Handle(new AdminArchiveUserCommand(targetId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        await _cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── PasswordChangedIntegrationEventHandler ────────────────────────────────

    [Fact]
    public async Task PasswordChanged_ShouldInvalidateSessionsCache_AfterSaveChanges()
    {
        var userId = Guid.NewGuid();

        var sessionRepo = Substitute.For<ISessionRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var inboxStore  = Substitute.For<IAuthInboxStore>();
        var uow         = Substitute.For<IAuthUnitOfWork>();
        var cache       = Substitute.For<HybridCache>();

        sessionRepo.GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Auth.Domain.Entities.Session, bool>>>(),
            include: Arg.Any<Func<IQueryable<Auth.Domain.Entities.Session>, IQueryable<Auth.Domain.Entities.Session>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Auth.Domain.Entities.Session>, IOrderedQueryable<Auth.Domain.Entities.Session>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(new List<Auth.Domain.Entities.Session>());

        refreshRepo.GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Auth.Domain.Entities.RefreshToken, bool>>>(),
            include: Arg.Any<Func<IQueryable<Auth.Domain.Entities.RefreshToken>, IQueryable<Auth.Domain.Entities.RefreshToken>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Auth.Domain.Entities.RefreshToken>, IOrderedQueryable<Auth.Domain.Entities.RefreshToken>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(new List<Auth.Domain.Entities.RefreshToken>());

        inboxStore.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = new PasswordChangedIntegrationEventHandler(
            sessionRepo, refreshRepo, inboxStore, uow, cache,
            NullLogger<PasswordChangedIntegrationEventHandler>.Instance);

        var ev = new PasswordChangedIntegrationEvent(userId);
        var notification = new IntegrationEventNotification<PasswordChangedIntegrationEvent>(
            Guid.NewGuid(), ev);

        await sut.Handle(notification, CancellationToken.None);

        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasswordChanged_ShouldNotInvalidateCache_WhenSaveChangesFails()
    {
        var userId = Guid.NewGuid();

        var sessionRepo = Substitute.For<ISessionRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var inboxStore  = Substitute.For<IAuthInboxStore>();
        var uow         = Substitute.For<IAuthUnitOfWork>();
        var cache       = Substitute.For<HybridCache>();

        sessionRepo.GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Auth.Domain.Entities.Session, bool>>>(),
            include: Arg.Any<Func<IQueryable<Auth.Domain.Entities.Session>, IQueryable<Auth.Domain.Entities.Session>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Auth.Domain.Entities.Session>, IOrderedQueryable<Auth.Domain.Entities.Session>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(new List<Auth.Domain.Entities.Session>());

        refreshRepo.GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Auth.Domain.Entities.RefreshToken, bool>>>(),
            include: Arg.Any<Func<IQueryable<Auth.Domain.Entities.RefreshToken>, IQueryable<Auth.Domain.Entities.RefreshToken>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Auth.Domain.Entities.RefreshToken>, IOrderedQueryable<Auth.Domain.Entities.RefreshToken>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(new List<Auth.Domain.Entities.RefreshToken>());

        inboxStore.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB failed"));

        var sut = new PasswordChangedIntegrationEventHandler(
            sessionRepo, refreshRepo, inboxStore, uow, cache,
            NullLogger<PasswordChangedIntegrationEventHandler>.Instance);

        var ev = new PasswordChangedIntegrationEvent(userId);
        var notification = new IntegrationEventNotification<PasswordChangedIntegrationEvent>(
            Guid.NewGuid(), ev);

        Func<Task> act = () => sut.Handle(notification, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        await cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasswordChanged_ShouldNotInvalidateCache_WhenAlreadyProcessed()
    {
        var userId = Guid.NewGuid();

        var sessionRepo = Substitute.For<ISessionRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var inboxStore  = Substitute.For<IAuthInboxStore>();
        var uow         = Substitute.For<IAuthUnitOfWork>();
        var cache       = Substitute.For<HybridCache>();

        inboxStore.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true); // Already processed → handler short-circuits.

        var sut = new PasswordChangedIntegrationEventHandler(
            sessionRepo, refreshRepo, inboxStore, uow, cache,
            NullLogger<PasswordChangedIntegrationEventHandler>.Instance);

        var ev = new PasswordChangedIntegrationEvent(userId);
        var notification = new IntegrationEventNotification<PasswordChangedIntegrationEvent>(
            Guid.NewGuid(), ev);

        await sut.Handle(notification, CancellationToken.None);

        // Idempotent re-delivery must not save and must not invalidate cache.
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
