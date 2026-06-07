namespace YallaJo.Web.Areas.Admin.Models.Growth;

// §8.8 Growth tools — Analytics admin. Base: /api/v1/analytics/admin.
// Enums (EntityType) are serialized by name; the API accepts the string form.

// ── Responses ────────────────────────────────────────────────────────────────

public sealed class SeasonalityRuleResponse
{
    public Guid Id { get; set; }
    public Guid PlaceId { get; set; }
    public int MonthStart { get; set; }
    public int MonthEnd { get; set; }
    public decimal Multiplier { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed class HolidayCalendarResponse
{
    public Guid Id { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Year { get; set; }
    public string? BoostRulesJson { get; set; }
    public bool IsActive { get; set; }
}

// ── Request bodies (mirror backend records) ───────────────────────────────────

public sealed record CreateSeasonalityRuleApiRequest(
    Guid PlaceId, int MonthStart, int MonthEnd, decimal Multiplier, string? Description);

public sealed record CreateHolidayCalendarApiRequest(
    string HolidayName, DateOnly StartDate, DateOnly EndDate, int Year, string? BoostRulesJson);

public sealed record SetPhotogenicApiRequest(bool IsPhotogenic);

public sealed record CreateExperimentApiRequest(
    string Name, DateTime StartsAt, DateTime ExpiresAt, int TrafficPercent, string VariantsJson);

// ── Form view-models (bound from the Growth views) ────────────────────────────

public sealed class CreateSeasonalityRuleVm
{
    public Guid PlaceId { get; set; }
    public int MonthStart { get; set; }
    public int MonthEnd { get; set; }
    public decimal Multiplier { get; set; } = 1.0m;
    public string? Description { get; set; }
}

public sealed class CreateHolidayVm
{
    public string HolidayName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public string? BoostRulesJson { get; set; }
}

public sealed class CreateExperimentVm
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public int TrafficPercent { get; set; } = 100;
    public string VariantsJson { get; set; } = "[]";
}

// ── Page view-model ───────────────────────────────────────────────────────────

public sealed class GrowthVm
{
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public IReadOnlyList<SeasonalityRuleResponse> SeasonalityRules { get; set; } = [];
    public IReadOnlyList<HolidayCalendarResponse> Holidays { get; set; } = [];
    public bool SeasonalityLoadFailed { get; set; }
    public bool HolidaysLoadFailed { get; set; }
}
