using Auth.Application.Caching;
using Auth.Application.Commands.ForceRevokeUserSessions;
using Auth.Application.Errors;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// AUTH-STD-6 — locks in the cross-module hierarchy guard for
/// <see cref="ForceRevokeUserSessionsCommandHandler"/>. The endpoint
/// enforces <c>User.UpdateAny</c>, but permission alone does not enforce
/// role-hierarchy: without this guard, an actor holding
/// <c>User.UpdateAny</c> could force-revoke sessions belonging to
/// Owner/SuperAdmin.
/// </summary>
public sealed class ForceRevokeUserSessionsCommandHandlerTests
{
    private readonly ISessionRepository      _sessionRepository      = Substitute.For<ISessionRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthUnitOfWork         _unitOfWork             = Substitute.For<IAuthUnitOfWork>();
    private readonly ISecurityService        _securityService        = Substitute.For<ISecurityService>();
    private readonly ICurrentUser            _currentUser            = Substitute.For<ICurrentUser>();
    private readonly HybridCache             _cache                  = Substitute.For<HybridCache>();
    private readonly Guid                    _actorId                = Guid.NewGuid();

    public ForceRevokeUserSessionsCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_actorId);

        // Default: empty repository results so revoke loops are no-ops.
        _sessionRepository.GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Session, bool>>>(),
            include: Arg.Any<Func<IQueryable<Session>, IQueryable<Session>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Session>, IOrderedQueryable<Session>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(new List<Session>());

        _refreshTokenRepository.GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
            include: Arg.Any<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
            orderBy: Arg.Any<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken>());
    }

    private ForceRevokeUserSessionsCommandHandler CreateSut() =>
        new(_sessionRepository, _refreshTokenRepository, _unitOfWork,
            _securityService, _currentUser, _cache);

    // ── 1. Forbidden when actor cannot manage target ──────────────────────────

    [Fact]
    public async Task ForceRevokeUserSessions_ShouldReturnForbidden_WhenActorCannotManageTarget()
    {
        var targetUserId = Guid.NewGuid();
        var forbiddenError = new Error("Forbidden.InsufficientPrivilege",
            "You do not have sufficient privilege to manage this user.");

        _securityService.EnsureCanManageUserAsync(_actorId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(forbiddenError, Outcome.Forbidden));

        var sut = CreateSut();

        var result = await sut.Handle(new ForceRevokeUserSessionsCommand(targetUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
        // Outcome, Messages, Errors must all be preserved verbatim — the
        // guard's Result is returned directly.
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(forbiddenError.Code);

        await _sessionRepository.DidNotReceive().GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Session, bool>>>(),
            include: Arg.Any<Func<IQueryable<Session>, IQueryable<Session>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Session>, IOrderedQueryable<Session>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>());

        await _refreshTokenRepository.DidNotReceive().GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
            include: Arg.Any<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
            orderBy: Arg.Any<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>());

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── 2. Happy path — guard allows, revocation + cache invalidation ─────────

    [Fact]
    public async Task ForceRevokeUserSessions_ShouldRevoke_WhenActorCanManageTarget()
    {
        var targetUserId = Guid.NewGuid();

        _securityService.EnsureCanManageUserAsync(_actorId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = CreateSut();

        var result = await sut.Handle(new ForceRevokeUserSessionsCommand(targetUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _sessionRepository.Received(1).GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Session, bool>>>(),
            include: Arg.Any<Func<IQueryable<Session>, IQueryable<Session>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Session>, IOrderedQueryable<Session>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>());

        await _refreshTokenRepository.Received(1).GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
            include: Arg.Any<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
            orderBy: Arg.Any<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // Cache invalidation MUST happen after SaveChangesAsync.
        await _cache.Received(1).RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(targetUserId),
            Arg.Any<CancellationToken>());
    }

    // ── 3. Unauthorized when current user is missing ──────────────────────────

    [Fact]
    public async Task ForceRevokeUserSessions_ShouldReturnUnauthorized_WhenCurrentUserMissing()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = CreateSut();

        var result = await sut.Handle(new ForceRevokeUserSessionsCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(AuthErrors.AdminUnauthenticated.Code);

        // Security contract must not have been called at all.
        await _securityService.DidNotReceive().EnsureCanManageUserAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        // No revocation, no save, no cache invalidation.
        await _sessionRepository.DidNotReceive().GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Session, bool>>>(),
            include: Arg.Any<Func<IQueryable<Session>, IQueryable<Session>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Session>, IOrderedQueryable<Session>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── 4. NotFound when guard reports the target is missing ──────────────────

    [Fact]
    public async Task ForceRevokeUserSessions_ShouldReturnNotFound_WhenTargetUserMissing()
    {
        var targetUserId = Guid.NewGuid();
        var notFoundError = new Error("NotFound.User", "The specified user was not found.");

        _securityService.EnsureCanManageUserAsync(_actorId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(notFoundError, Outcome.NotFound));

        var sut = CreateSut();

        var result = await sut.Handle(new ForceRevokeUserSessionsCommand(targetUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(notFoundError.Code);

        await _sessionRepository.DidNotReceive().GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<Session, bool>>>(),
            include: Arg.Any<Func<IQueryable<Session>, IQueryable<Session>>?>(),
            orderBy: Arg.Any<Func<IQueryable<Session>, IOrderedQueryable<Session>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().GetAllAsync(
            Arg.Any<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>(),
            include: Arg.Any<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
            orderBy: Arg.Any<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
            asNoTracking: Arg.Any<bool>(),
            ct: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
