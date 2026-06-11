namespace YallaJo.Web.Areas.Admin.Models.Shared;

/// <summary>
/// View model for the shared <c>Partials/_FilterCard</c> partial — the standard
/// GET filter form rendered inside a card (D1, PE1: plain form submit, no JS needed).
/// The caller passes pre-localized strings and pre-resolved URLs (<c>Url.Action</c>).
/// Complex filter forms that do not fit this shape stay bespoke.
/// </summary>
public sealed class FilterCardVm
{
    public required string Heading { get; init; }

    /// <summary>DOM id of the heading; the card section references it via <c>aria-labelledby</c>.</summary>
    public string HeadingId { get; init; } = "filter-heading";

    /// <summary>GET form target, computed via <c>Url.Action(...)</c> at the call site.</summary>
    public required string FormAction { get; init; }

    public required string SubmitText { get; init; }

    /// <summary>Optional reset link (rendered only when both URL and text are present).</summary>
    public string? ResetUrl { get; init; }

    public string? ResetText { get; init; }

    public IReadOnlyList<FilterFieldVm> Fields { get; init; } = [];

    /// <summary>Filter values that must survive a re-submit but have no visible field.</summary>
    public IReadOnlyList<HiddenFieldVm> HiddenFields { get; init; } = [];
}

/// <summary>One visible field inside <see cref="FilterCardVm"/>.</summary>
public sealed class FilterFieldVm
{
    public required string Name { get; init; }

    public required string Label { get; init; }

    public FilterFieldKind Kind { get; init; } = FilterFieldKind.Select;

    /// <summary>Current value for search/date/text inputs.</summary>
    public string? Value { get; init; }

    public string? Placeholder { get; init; }

    /// <summary>Options for <see cref="FilterFieldKind.Select"/> fields.</summary>
    public IReadOnlyList<FilterOptionVm> Options { get; init; } = [];

    /// <summary>Bootstrap column classes controlling the field's width.</summary>
    public string ColumnClass { get; init; } = "col-sm-6 col-md-3";
}

public enum FilterFieldKind
{
    Select,
    Search,
    Date,
    Text,
}

public sealed record FilterOptionVm(string Value, string Text, bool Selected);
