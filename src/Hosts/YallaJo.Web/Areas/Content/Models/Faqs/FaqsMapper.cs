namespace YallaJo.Web.Areas.Content.Models.Faqs;
public static class FaqsMapper
{
    public static FaqListVm ToListVm(FaqPageResponse page)
    {
        var groups = page.Items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.EntityType) ? "General" : i.EntityType)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FaqGroupVm
            {
                Key         = g.Key,
                DisplayName = Humanize(g.Key),
                Items       = g.OrderBy(i => i.SortOrder)
                               .Select(ToItemVm)
                               .ToList(),
            })
            .ToList();

        return new FaqListVm
        {
            Groups          = groups,
            PageNumber      = page.PageNumber,
            PageSize        = page.PageSize,
            TotalCount      = page.TotalCount,
            TotalPages      = page.TotalPages,
            HasPreviousPage = page.HasPreviousPage,
            HasNextPage     = page.HasNextPage,
        };
    }

    private static FaqItemVm ToItemVm(FaqItemResponse r) => new()
    {
        Id        = r.Id,
        Question  = r.Question,
        Answer    = r.Answer,
        SortOrder = r.SortOrder,
    };

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "General";

        var sb = new System.Text.StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
