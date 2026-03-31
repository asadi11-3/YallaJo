namespace ContentCore.Presentation.Endpoints.Category.Models;

public sealed record ReorderCategoriesRequest(
    IReadOnlyList<ReorderCategorySortOrderRequest> SortOrders);
