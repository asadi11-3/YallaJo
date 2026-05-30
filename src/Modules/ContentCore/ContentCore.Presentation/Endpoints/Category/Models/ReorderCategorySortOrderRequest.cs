namespace ContentCore.Presentation.Endpoints.Category.Models;

/// <summary>A single category sort order entry used in the reorder batch request.</summary>
public sealed record ReorderCategorySortOrderRequest(Guid CategoryId, int SortOrder);
