namespace YallaJo.Web.Areas.Content.Models.Faqs;

public sealed class FaqItemVm
{
    public Guid Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
