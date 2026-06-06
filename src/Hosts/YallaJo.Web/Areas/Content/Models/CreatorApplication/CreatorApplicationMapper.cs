using YallaJo.Web.Areas.Content.Models.Blogs;

namespace YallaJo.Web.Areas.Content.Models.CreatorApplication;

public static class CreatorApplicationMapper
{
    private static readonly char[] LineSeparators = ['\n', '\r'];

    public static IReadOnlyList<NicheChoiceVm> ToNicheChoices(
        IEnumerable<CreatorNicheResponse> niches,
        IReadOnlyCollection<Guid>? selectedIds = null)
        => niches
            .Where(n => n.IsActive)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Name)
            .Select(n => new NicheChoiceVm
            {
                Id = n.Id,
                Name = n.Name,
                Description = n.Description,
                Selected = selectedIds is not null && selectedIds.Contains(n.Id),
            })
            .ToList();

    public static CreateCreatorApplicationApiRequest ToCreateRequest(CreatorApplicationFormVm form)
        => new(
            Bio: NullIfBlank(form.Bio),
            PortfolioUrls: ToUrlList(form.PortfolioUrls),
            SampleWorkUrls: ToUrlList(form.SampleWorkUrls),
            NicheIds: form.NicheIds.Count > 0 ? form.NicheIds : null,
            FreeTags: ToTagList(form.FreeTags),
            LanguageIds: null,
            PreferredRegionIds: null,
            SocialHandles: ToSocialHandles(form.SocialHandles));

    public static UpdateCreatorApplicationApiRequest ToUpdateRequest(CreatorApplicationFormVm form)
        => new(
            Bio: NullIfBlank(form.Bio),
            PortfolioUrls: ToUrlList(form.PortfolioUrls),
            SampleWorkUrls: ToUrlList(form.SampleWorkUrls),
            NicheIds: form.NicheIds.Count > 0 ? form.NicheIds : null,
            FreeTags: ToTagList(form.FreeTags),
            LanguageIds: null,
            PreferredRegionIds: null,
            SocialHandles: ToSocialHandles(form.SocialHandles));

    public static CreatorApplicationFormVm ToForm(CreatorApplicationResponse application)
        => new()
        {
            Id = application.Id,
            Bio = application.Bio,
            PortfolioUrls = JoinLines(application.PortfolioUrls),
            SampleWorkUrls = JoinLines(application.SampleWorkUrls),
            NicheIds = application.NicheIds.ToList(),
            FreeTags = application.FreeTags.Count > 0 ? string.Join(", ", application.FreeTags) : null,
            SocialHandles = application.SocialHandles.Count > 0
                ? string.Join("\n", application.SocialHandles.Select(kv => $"{kv.Key}: {kv.Value}"))
                : null,
        };

    public static CreatorApplicationStatusVm ToStatusVm(
        CreatorApplicationResponse application,
        IEnumerable<CreatorNicheResponse> niches)
    {
        var editable = IsEditableStatus(application.Status);
        return new CreatorApplicationStatusVm
        {
            HasApplication = true,
            Id = application.Id,
            Status = application.Status,
            StatusLabel = StatusLabel(application.Status),
            AdminNote = application.AdminNote,
            RejectionReason = application.RejectionReason,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdatedAt,
            IsEditable = editable,
            CanSubmit = editable,
            Form = ToForm(application),
            Niches = ToNicheChoices(niches, application.NicheIds),
        };
    }

    /// <summary>Status colour for badges; never the sole indicator (always paired with a text label).</summary>
    public static string StatusColor(string? status) => status?.ToLowerInvariant() switch
    {
        "approved" => "success",
        "pending" => "warning",
        "moreinfoneeded" => "warning",
        "rejected" => "danger",
        "draft" => "secondary",
        _ => "secondary",
    };

    public static string StatusLabel(string? status) => status?.ToLowerInvariant() switch
    {
        "approved" => "Approved",
        "pending" => "Pending review",
        "moreinfoneeded" => "More info needed",
        "rejected" => "Rejected",
        "draft" => "Draft",
        _ => status ?? "Unknown",
    };

    private static bool IsEditableStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "draft" => true,
        "moreinfoneeded" => true,
        _ => false,
    };

    private static List<string>? ToUrlList(string? raw)
    {
        var list = SplitLines(raw);
        return list.Count > 0 ? list : null;
    }

    private static List<string>? ToTagList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var list = raw
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return list.Count > 0 ? list : null;
    }

    private static Dictionary<string, string>? ToSocialHandles(string? raw)
    {
        var lines = SplitLines(raw);
        if (lines.Count == 0) return null;

        var handles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var idx = line.IndexOf(':');
            if (idx <= 0 || idx >= line.Length - 1) continue;
            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (key.Length > 0 && value.Length > 0)
            {
                handles[key] = value;
            }
        }

        return handles.Count > 0 ? handles : null;
    }

    private static List<string> SplitLines(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? JoinLines(IReadOnlyList<string> values)
        => values.Count > 0 ? string.Join("\n", values) : null;

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
