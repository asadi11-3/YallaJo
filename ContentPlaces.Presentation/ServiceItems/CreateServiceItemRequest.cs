using System;

namespace ContentPlaces.Presentation.ServiceItems;

// هاد الـ DTO بيمثل الداتا اللي الزبون ببعثها بالـ JSON
// لاحظ إننا ما حطينا BusinessId هون، لأنه رح ناخذه من الرابط
public sealed record CreateServiceItemRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    int SortOrder
);
