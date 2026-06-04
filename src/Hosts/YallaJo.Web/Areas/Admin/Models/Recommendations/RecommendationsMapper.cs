namespace YallaJo.Web.Areas.Admin.Models.Recommendations;

public static class RecommendationsMapper
{
    public static RecommendationsVm ToVm(IReadOnlyList<BatchResponse> batches)
    {
        return new RecommendationsVm
        {
            Batches = batches.Select(b => new BatchRowVm
            {
                Id = b.Id,
                SourceKind = b.SourceKind,
                SourceId = b.SourceId,
                Context = b.Context,
                AlgorithmVersion = b.AlgorithmVersion,
                ComputedAt = b.ComputedAt,
                IsStale = b.IsStale,
                ItemCount = b.ItemCount,
            }).ToList(),
        };
    }
}
