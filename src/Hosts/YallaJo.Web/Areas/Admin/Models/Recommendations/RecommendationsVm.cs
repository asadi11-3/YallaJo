namespace YallaJo.Web.Areas.Admin.Models.Recommendations;

public sealed class RecommendationsVm
{
    public IReadOnlyList<BatchRowVm> Batches { get; set; } = [];
}

public sealed class BatchRowVm
{
    public Guid Id { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string Context { get; set; } = string.Empty;
    public string AlgorithmVersion { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; }
    public bool IsStale { get; set; }
    public int ItemCount { get; set; }
}
