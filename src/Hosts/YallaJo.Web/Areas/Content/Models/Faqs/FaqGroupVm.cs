namespace YallaJo.Web.Areas.Content.Models.Faqs;

public sealed class FaqGroupVm
{
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyList<FaqItemVm> Items { get; init; } = [];
}
