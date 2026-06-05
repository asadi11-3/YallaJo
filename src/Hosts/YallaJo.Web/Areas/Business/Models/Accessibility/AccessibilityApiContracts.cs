namespace YallaJo.Web.Areas.Business.Models.Accessibility;

public enum AccessibilityFeatureType : byte
{
    Wheelchair = 0,
    Visual = 1,
    Hearing = 2,
    Cognitive = 3,
    Mobility = 4,
    Other = 5
}

public sealed class AccessibilityFeatureItemResponse
{
    public Guid Id { get; set; }
    public AccessibilityFeatureType FeatureType { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsAvailable { get; set; }
}

public sealed record AccessibilityFeatureItemApiRequest(
    AccessibilityFeatureType FeatureType,
    string Name,
    string? Description,
    bool IsAvailable);
