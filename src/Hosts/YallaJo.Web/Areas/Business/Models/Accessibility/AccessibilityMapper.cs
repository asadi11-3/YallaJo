namespace YallaJo.Web.Areas.Business.Models.Accessibility;

public static class AccessibilityMapper
{
    private static readonly (AccessibilityFeatureType Type, string Label, string DefaultName)[] FeatureCatalog =
    [
        (AccessibilityFeatureType.Wheelchair, "Wheelchair access", "Wheelchair access"),
        (AccessibilityFeatureType.Visual, "Visual assistance", "Visual assistance"),
        (AccessibilityFeatureType.Hearing, "Hearing assistance", "Hearing assistance"),
        (AccessibilityFeatureType.Cognitive, "Cognitive support", "Cognitive support"),
        (AccessibilityFeatureType.Mobility, "Mobility support", "Mobility support"),
        (AccessibilityFeatureType.Other, "Other", "Other accessibility feature")
    ];

    public static List<AccessibilityFeatureFormVm> ToFeatureRows(IReadOnlyList<AccessibilityFeatureItemResponse> existing)
    {
        var rows = new List<AccessibilityFeatureFormVm>(FeatureCatalog.Length);
        foreach (var (type, label, defaultName) in FeatureCatalog)
        {
            var match = existing.FirstOrDefault(f => f.FeatureType == type);
            rows.Add(new AccessibilityFeatureFormVm
            {
                FeatureType = type,
                FeatureLabel = label,
                Name = string.IsNullOrWhiteSpace(match?.Name) ? defaultName : match!.Name,
                Description = match?.Description,
                IsAvailable = match?.IsAvailable ?? false
            });
        }

        return rows;
    }
}
