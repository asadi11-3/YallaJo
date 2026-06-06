using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatureCatalog;

public sealed class GetAccessibilityFeatureCatalogQueryHandler
    : IQueryHandler<GetAccessibilityFeatureCatalogQuery, IReadOnlyList<AccessibilityFeatureCatalogItem>>
{
    private static readonly IReadOnlyList<AccessibilityFeatureCatalogItem> Catalog = BuildCatalog();

    public Task<Result<IReadOnlyList<AccessibilityFeatureCatalogItem>>> Handle(
        GetAccessibilityFeatureCatalogQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result<IReadOnlyList<AccessibilityFeatureCatalogItem>>.Success(Catalog));

    private static IReadOnlyList<AccessibilityFeatureCatalogItem> BuildCatalog()
    {
        // Names map enum members 1:1 to spec types (Wheelchair/Visual/Hearing/Cognitive/Mobility/Other).
        var values = Enum.GetValues<AccessibilityFeatureType>();
        var items = new List<AccessibilityFeatureCatalogItem>(values.Length);
        foreach (var v in values)
        {
            var code = v.ToString();
            items.Add(new AccessibilityFeatureCatalogItem(
                FeatureType: v,
                Code: code,
                DisplayName: code));
        }
        return items;
    }
}
