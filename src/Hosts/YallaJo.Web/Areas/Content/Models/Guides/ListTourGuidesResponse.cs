namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class ListTourGuidesResponse
{
    public List<TourGuideListItemResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
