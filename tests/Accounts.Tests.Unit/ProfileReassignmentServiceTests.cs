using System.Linq.Expressions;
using Accounts.Application.Caching;
using Accounts.Contracts.Abstractions;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Tests.Unit;

/// <summary>
/// Phase 3D — service-level tests for <c>ProfileReassignmentService</c>.
/// Verifies:
///   • Reset path: tracked profile is mutated via the domain method,
///     UoW flushes, cache tag evicted.
///   • Missing-profile path: idempotent Result.Success with log line
///     (no UoW save, no cache call).
///   • Cache eviction targets the correct per-user tag.
///   • Persistence exceptions propagate (no silent success).
/// </summary>
public sealed class ProfileReassignmentServiceTests
{
    private readonly IProfileRepository   _profileRepo = Substitute.For<IProfileRepository>();
    private readonly IAccountsUnitOfWork  _uow         = Substitute.For<IAccountsUnitOfWork>();
    private readonly HybridCache          _cache       = Substitute.For<HybridCache>();

    private IProfileReassignmentService CreateSut()
    {
        var assembly = typeof(Accounts.Application.DependencyInjection).Assembly;
        var type = assembly.GetType(
            "Accounts.Application.Services.ProfileReassignmentService",
            throwOnError: true)!;

        // Construct the generic ILogger<ProfileReassignmentService>
        // that the sealed internal service's primary constructor
        // requires. NullLogger<T>.Instance matches the generic arity.
        var logger = typeof(NullLogger<>).MakeGenericType(type)
            .GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

        return (IProfileReassignmentService)Activator.CreateInstance(
            type, _profileRepo, _uow, _cache, logger)!;
    }

    private void StubProfile(Profile? profile)
    {
        _profileRepo.FirstOrDefaultAsync(
                filter:       Arg.Any<Expression<Func<Profile, bool>>>(),
                include:      Arg.Any<Func<IQueryable<Profile>, IQueryable<Profile>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<Profile>, IOrderedQueryable<Profile>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
            .Returns(profile);
    }

    [Fact]
    public async Task ResetForReassignmentAsync_ShouldResetProfile_AndSave_WhenProfileExists()
    {
        var userId = Guid.NewGuid();
        var profile = Profile.Create(userId, "Alice", "Anderson");
        profile.SetAvatarUrl("https://cdn.example.com/a.png");
        StubProfile(profile);

        var sut = CreateSut();

        var result = await sut.ResetForReassignmentAsync(
            new ProfileReassignmentRequest(userId, "bob@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.FirstName.Should().Be("Pending");
        profile.LastName.Should().Be("Activation");
        profile.DisplayName.Should().Be("bob",
            "the service must derive DisplayName from the new email local-part");
        profile.AvatarUrl.Should().BeNull();

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetForReassignmentAsync_ShouldReturnSuccess_AndSkipSave_WhenProfileMissing()
    {
        var userId = Guid.NewGuid();
        StubProfile(null);

        var sut = CreateSut();

        var result = await sut.ResetForReassignmentAsync(
            new ProfileReassignmentRequest(userId, "bob@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(
            "reassignment must not fail just because the profile has not been provisioned yet");

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync(default(string)!, default);
    }

    [Fact]
    public async Task ResetForReassignmentAsync_ShouldEvictUserProfileCacheTag()
    {
        var userId = Guid.NewGuid();
        var profile = Profile.Create(userId, "Alice", "Anderson");
        StubProfile(profile);

        var expectedTag = AccountsCacheKeys.UserProfileTag(userId);

        var sut = CreateSut();

        await sut.ResetForReassignmentAsync(
            new ProfileReassignmentRequest(userId, "bob@example.com"),
            CancellationToken.None);

        await _cache.Received(1).RemoveByTagAsync(expectedTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetForReassignmentAsync_ShouldPropagateException_WhenSaveFails()
    {
        var userId = Guid.NewGuid();
        var profile = Profile.Create(userId, "Alice", "Anderson");
        StubProfile(profile);

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("db unavailable"));

        var sut = CreateSut();

        var act = async () => await sut.ResetForReassignmentAsync(
            new ProfileReassignmentRequest(userId, "bob@example.com"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("db unavailable",
                "persistence failures must bubble up so the outer TransactionScope rolls back — no silent success");

        // Cache eviction must NOT run if the save failed.
        await _cache.DidNotReceiveWithAnyArgs().RemoveByTagAsync(default(string)!, default);
    }

    [Fact]
    public async Task ResetForReassignmentAsync_ShouldReturnInvalid_WhenUserIdIsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.ResetForReassignmentAsync(
            new ProfileReassignmentRequest(Guid.Empty, "bob@example.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);

        await _profileRepo.DidNotReceiveWithAnyArgs().FirstOrDefaultAsync(
            filter:       default(Expression<Func<Profile, bool>>)!,
            include:      default,
            orderBy:      default,
            asNoTracking: default,
            ct:           default);
    }
}
