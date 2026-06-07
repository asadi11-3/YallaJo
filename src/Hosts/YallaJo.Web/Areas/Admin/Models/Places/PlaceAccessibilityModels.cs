namespace YallaJo.Web.Areas.Admin.Models.Places;

// §8.5 — accessibility-feature management for a place.
//
// Set:    PUT    /api/v1/places/{id}/accessibility   (batch replace)
//         body: AccessibilityFeatureItemApiRequest[]  (FeatureType serialized as its enum name)
// Remove: DELETE /api/v1/places/admin/accessibility/{assignmentId}

// Item in the batch-replace payload sent to the API.
public sealed record AccessibilityFeatureItemApiRequest(
    string FeatureType,
    string Name,
    string? Description,
    bool IsAvailable);

// Catalog row (GET /api/v1/places/accessibility/catalog).
public sealed class AccessibilityFeatureCatalogItemResponse
{
    public string FeatureType { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

// A place's current accessibility assignment (GET /api/v1/places/{id}/accessibility).
public sealed class AccessibilityFeatureResponse
{
    public Guid Id { get; set; }
    public string FeatureType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAvailable { get; set; }
}

// Bound from the place details accessibility editor form (one feature row).
public sealed class AccessibilityFeatureFormItem
{
    public string FeatureType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAvailable { get; set; } = true;
}
