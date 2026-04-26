using ContentPlaces.Domain.Enums;
using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Tests.Unit;

internal static class TestServiceItemFactory
{
    public static ServiceItemEntity CreateActiveServiceItem(Guid businessId, string name = "Test Service")
        => ServiceItemEntity.Create(
            businessId: businessId,
            name: name,
            price: 100m,
            currency: "JOD",
            category: ServiceCategory.Activity,
            durationMinutes: 60,
            maxCapacity: 10,
            description: "Test description",
            sortOrder: 0);
}
