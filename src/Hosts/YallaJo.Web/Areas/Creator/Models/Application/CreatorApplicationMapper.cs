using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace YallaJo.Web.Areas.Creator.Models.Application;

public static class CreatorApplicationMapper
{
    private const int MaxUrls = 10;
    private const int MaxNiches = 5;
    private const int MaxFreeTags = 20;
    private const int MaxFreeTagLength = 50;

    public static IReadOnlyList<CreatorNicheOptionVm> ToNicheOptions(
        IEnumerable<CreatorNicheResponse>? niches)
        => niches is null
            ? []
            : niches
                .Where(n => n.IsActive)
                .OrderBy(n => n.SortOrder)
                .Select(n => new CreatorNicheOptionVm
                {
                    Id          = n.Id,
                    Name        = n.Name,
                    Description = n.Description,
                })
                .ToList();

    public static CreatorApplicationFormVm ToFormVm(
        CreatorApplicationMineResponse? application,
        IReadOnlyList<CreatorNicheOptionVm> nicheOptions)
    {
        if (application is null)
            return new CreatorApplicationFormVm { NicheOptions = nicheOptions };

        return new CreatorApplicationFormVm
        {
            ApplicationId      = application.Id,
            Status             = application.Status,
            AdminNote          = application.AdminNote,
            Bio                = application.Bio,
            PortfolioUrlsText  = JoinLines(application.PortfolioUrls),
            SampleWorkUrlsText = JoinLines(application.SampleWorkUrls),
            FreeTagsText       = JoinCommas(application.FreeTags),
            SelectedNicheIds   = application.NicheIds.ToList(),
            NicheOptions       = nicheOptions,
        };
    }

    public static CreateCreatorApplicationRequestBody ToCreateBody(CreatorApplicationFormVm form)
        => new(
            Bio: Trimmed(form.Bio),
            PortfolioUrls: SplitLines(form.PortfolioUrlsText, MaxUrls),
            SampleWorkUrls: SplitLines(form.SampleWorkUrlsText, MaxUrls),
            NicheIds: LimitNiches(form.SelectedNicheIds),
            FreeTags: SplitCommas(form.FreeTagsText, MaxFreeTags, MaxFreeTagLength),
            LanguageIds: [],
            PreferredRegionIds: [],
            SocialHandles: new Dictionary<string, string>());

    public static UpdateCreatorApplicationRequestBody ToUpdateBody(CreatorApplicationFormVm form)
        => new(
            Bio: Trimmed(form.Bio),
            PortfolioUrls: SplitLines(form.PortfolioUrlsText, MaxUrls),
            SampleWorkUrls: SplitLines(form.SampleWorkUrlsText, MaxUrls),
            NicheIds: LimitNiches(form.SelectedNicheIds),
            FreeTags: SplitCommas(form.FreeTagsText, MaxFreeTags, MaxFreeTagLength),
            LanguageIds: [],
            PreferredRegionIds: [],
            SocialHandles: new Dictionary<string, string>());


    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string JoinLines(IReadOnlyList<string> values) => string.Join("\n", values);

    private static string JoinCommas(IReadOnlyList<string> values) => string.Join(", ", values);

    private static IReadOnlyList<string> SplitLines(string? text, int max)
        => SplitBy(text, ['\n', '\r'], max, maxItemLength: null);

    private static IReadOnlyList<string> SplitCommas(string? text, int max, int maxItemLength)
        => SplitBy(text, [','], max, maxItemLength);

    private static IReadOnlyList<string> SplitBy(
        string? text, char[] separators, int max, int? maxItemLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var items = text
            .Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => maxItemLength is { } len && s.Length > len ? s[..len] : s)
            .Take(max)
            .ToList();

        return items;
    }

    private static IReadOnlyList<Guid> LimitNiches(IReadOnlyList<Guid> nicheIds)
        => nicheIds.Distinct().Take(MaxNiches).ToList();
}
