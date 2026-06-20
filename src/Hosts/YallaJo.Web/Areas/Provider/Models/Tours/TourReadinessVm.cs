namespace YallaJo.Web.Areas.Provider.Models.Tours;

/// <summary>
/// Persisted-data view of a draft listing's submit-readiness, mirroring the
/// authoritative backend pre-submit gate (<c>SubmitTourCommandHandler</c>).
/// <para>
/// Each flag is computed from saved data (detail + active pricing + active
/// schedules + images), NOT from which wizard step the user happened to visit.
/// The <see cref="MissingRequirementKeys"/> are resource keys for the items
/// still blocking submit, so the Review &amp; Submit panel can render a precise
/// checklist. The backend remains the source of truth; this is a UX aid that
/// short-circuits a guaranteed-to-fail submit and explains why.
/// </para>
/// </summary>
public sealed class TourReadinessVm
{
    public Guid TourId { get; init; }

    /// <summary>Basics: a Place is linked, a meeting point exists, and the description meets the minimum length.</summary>
    public bool BasicsComplete { get; init; }

    /// <summary>At least one active pricing tier AND at least one active Adult tier exist.</summary>
    public bool PricingComplete { get; init; }

    /// <summary>At least one active schedule exists.</summary>
    public bool ScheduleComplete { get; init; }

    /// <summary>At least one image is attached.</summary>
    public bool ImagesComplete { get; init; }

    /// <summary>
    /// True when the readiness data could not be fully resolved (e.g. a sub-resource
    /// lookup failed). The panel then renders a soft warning instead of a false
    /// "incomplete" and never blocks submit on its own — the backend still decides.
    /// </summary>
    public bool IsDegraded { get; init; }

    /// <summary>Resource keys (SharedResource) for each missing requirement, in display order.</summary>
    public IReadOnlyList<string> MissingRequirementKeys { get; init; } = [];

    public bool AllComplete => BasicsComplete && PricingComplete && ScheduleComplete && ImagesComplete;

    /// <summary>
    /// Per-step completion for the wizard stepper (1=Basics, 2=Pricing, 3=Schedule,
    /// 4=Images). Step 5 (Review &amp; submit) is considered done only when all are met.
    /// </summary>
    public IReadOnlyDictionary<int, bool> StepCompletion => new Dictionary<int, bool>
    {
        [1] = BasicsComplete,
        [2] = PricingComplete,
        [3] = ScheduleComplete,
        [4] = ImagesComplete,
        [5] = AllComplete,
    };
}
