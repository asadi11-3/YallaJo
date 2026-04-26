using ContentPlaces.Application.Caching;
using FluentAssertions;

namespace ContentPlaces.Tests.Unit;

public sealed class ContentPlacesCacheKeysTests
{
    [Fact]
    public void ServiceItemList_VariesByElevatedFlag()
    {
        var businessId = Guid.NewGuid();
        var publicKey = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: false);
        var elevatedKey = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: true);

        publicKey.Should().NotBe(elevatedKey);
    }

    [Fact]
    public void ServiceItemList_VariesByBusinessId()
    {
        var keyA = ContentPlacesCacheKeys.ServiceItemList(Guid.NewGuid(), isElevated: true);
        var keyB = ContentPlacesCacheKeys.ServiceItemList(Guid.NewGuid(), isElevated: true);

        keyA.Should().NotBe(keyB);
    }

    [Fact]
    public void ServiceItemList_StableForSameInputs()
    {
        var businessId = Guid.NewGuid();
        var keyA = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: false);
        var keyB = ContentPlacesCacheKeys.ServiceItemList(businessId, isElevated: false);

        keyA.Should().Be(keyB);
    }
}
