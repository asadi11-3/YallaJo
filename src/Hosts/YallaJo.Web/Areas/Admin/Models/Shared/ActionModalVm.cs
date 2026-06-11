namespace YallaJo.Web.Areas.Admin.Models.Shared;

/// <summary>
/// View model for the shared <c>Partials/_ActionModal</c> partial — the standard
/// confirm/act modal posting a simple form (MOD3, MOD5, PE1, SEC7). Covers the
/// common approve/suspend/close shapes: a message, an optional reason textarea and
/// optional hidden fields. Rich modals (refunds, reassignment, claim matrices)
/// stay bespoke. Permission gating stays at the call site.
/// </summary>
public sealed class ActionModalVm
{
    /// <summary>Modal DOM id; the header/body ids derive from it ("{Id}Label" / "{Id}Body").</summary>
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Pre-localized accessible label for the close (×) button.</summary>
    public required string CloseLabel { get; init; }

    public required string CancelText { get; init; }

    public required string SubmitText { get; init; }

    /// <summary>Bootstrap button class for the submit action, e.g. <c>btn-danger</c>.</summary>
    public string SubmitClass { get; init; } = "btn-danger";

    /// <summary>Destructive header variant (<c>bg-danger text-white</c>).</summary>
    public bool DangerHeader { get; init; }

    /// <summary>Static backdrop + keyboard off — required for destructive flows (MOD3).</summary>
    public bool StaticBackdrop { get; init; }

    /// <summary>Renders <c>role="alertdialog"</c> instead of <c>role="dialog"</c> (MOD4).</summary>
    public bool AlertDialog { get; init; }

    /// <summary>POST form target, computed via <c>Url.Action(...)</c> at the call site.</summary>
    public required string FormAction { get; init; }

    /// <summary>Optional pre-localized body message paragraph.</summary>
    public string? Message { get; init; }

    /// <summary>Optional reason textarea configuration.</summary>
    public ReasonFieldVm? Reason { get; init; }

    public IReadOnlyList<HiddenFieldVm> HiddenFields { get; init; } = [];
}

/// <summary>Reason textarea inside <see cref="ActionModalVm"/>.</summary>
public sealed class ReasonFieldVm
{
    public required string Label { get; init; }

    public string? Help { get; init; }

    public string Name { get; init; } = "reason";

    public int Rows { get; init; } = 3;

    public int MaxLength { get; init; } = 1000;

    public bool Required { get; init; } = true;
}

/// <summary>Hidden input rendered inside a shared partial's form.</summary>
public sealed record HiddenFieldVm(string Name, string Value);
