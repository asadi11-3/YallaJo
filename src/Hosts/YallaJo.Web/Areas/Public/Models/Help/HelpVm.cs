namespace YallaJo.Web.Areas.Public.Models.Help;

public sealed class FaqVm
{
    public Guid Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
}

public sealed class HelpVm
{
    public IReadOnlyList<FaqVm> Faqs { get; init; } = [];
    public bool HasFaqs => Faqs.Count > 0;
}

public sealed class HelpDetailVm
{
    public Guid Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
}
