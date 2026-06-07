using Accounts.Contracts.Abstractions;
using Accounts.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Accounts.Tests.Unit;

/// <summary>
/// Covers the backfill data path used to issue the server-generated <c>provider_id</c>
/// identity claim: the service must surface every approved provider's (UserId, ProviderId)
/// pair, where ProviderId is the approved ProviderApplication.Id.
/// </summary>
public sealed class ProviderStatusServiceTests
{
    private readonly IProviderApplicationRepository _repo =
        Substitute.For<IProviderApplicationRepository>();

    // ProviderStatusService is internal sealed — construct it by reflection through
    // the public IProviderStatusService contract (mirrors ProfileReassignmentServiceTests).
    private IProviderStatusService CreateSut()
    {
        var assembly = typeof(Accounts.Application.DependencyInjection).Assembly;
        var type = assembly.GetType(
            "Accounts.Application.Services.ProviderStatusService",
            throwOnError: true)!;

        return (IProviderStatusService)Activator.CreateInstance(type, _repo)!;
    }

    [Fact]
    public async Task GetApprovedProviderClaimsAsync_MapsUserAndProviderPairs()
    {
        var u1 = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        _repo.GetApprovedUserProviderPairsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<(Guid UserId, Guid ProviderId)> { (u1, p1), (u2, p2) });

        var sut = CreateSut();

        var result = await sut.GetApprovedProviderClaimsAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(c => c.UserId == u1 && c.ProviderId == p1);
        result.Should().ContainSingle(c => c.UserId == u2 && c.ProviderId == p2);
    }

    [Fact]
    public async Task GetApprovedProviderClaimsAsync_ReturnsEmpty_WhenNoApprovedProviders()
    {
        _repo.GetApprovedUserProviderPairsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<(Guid UserId, Guid ProviderId)>());

        var sut = CreateSut();

        var result = await sut.GetApprovedProviderClaimsAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}
