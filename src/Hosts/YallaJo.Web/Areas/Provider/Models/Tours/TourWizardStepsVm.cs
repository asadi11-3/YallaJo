namespace YallaJo.Web.Areas.Provider.Models.Tours;

/// <summary>
/// Drives the guided 5-step listing-setup stepper (PROV1-6 / F4) rendered across the
/// Tours editor + its sub-resource pages. The flow is API-correct: step 1 creates the
/// draft, steps 2–4 manage sub-resources by tour id, step 5 reviews + submits. The
/// component is a navigation/progress aid (PE1 — plain links), not a monolithic form.
/// </summary>
public sealed class TourWizardStepsVm
{
    public Guid TourId { get; init; }

    /// <summary>1 = Basics, 2 = Pricing, 3 = Schedule, 4 = Images, 5 = Review &amp; submit.</summary>
    public int CurrentStep { get; init; }

    /// <summary>
    /// Persisted-data completion per step number. When supplied, the stepper marks a
    /// step done because the underlying data exists — not merely because the user
    /// walked past it. When <c>null</c>, the stepper has no data signal and shows no
    /// check marks (it will not fall back to the old "visited == done" heuristic).
    /// </summary>
    public IReadOnlyDictionary<int, bool>? Completion { get; init; }

    /// <summary>True when this step's persisted data is complete (defaults to false when unknown).</summary>
    public bool IsStepDone(int number) =>
        Completion is not null && Completion.TryGetValue(number, out var done) && done;

    public sealed record Step(int Number, string Label, string Icon, string Controller, string Action);

    public static IReadOnlyList<Step> Steps { get; } =
    [
        new(1, "Basics",          "bi-card-text",      "Tours",         "Edit"),
        new(2, "Pricing",         "bi-tag",            "TourPricing",   "Index"),
        new(3, "Schedule",        "bi-calendar-event", "TourSchedules", "Index"),
        new(4, "Images",          "bi-images",         "TourImages",    "Index"),
        new(5, "Review & submit", "bi-send",           "Tours",         "Edit"),
    ];
}
