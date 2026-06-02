namespace YallaJo.Web.Areas.Content.Models.Faqs;

public sealed class FaqListVm
{
    public IReadOnlyList<FaqGroupVm> Groups { get; init; } = [];

    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public bool HasFaqs => Groups.Count > 0;
}
