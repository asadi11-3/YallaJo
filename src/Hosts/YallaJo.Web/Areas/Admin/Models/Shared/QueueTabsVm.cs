namespace YallaJo.Web.Areas.Admin.Models.Shared;

/// <summary>
/// View model for the shared queue status tabs partial
/// (<c>Views/Shared/Partials/_QueueTabs.cshtml</c>). Tabs are plain GET links
/// (PE1) so they work without JavaScript; on pages whose list is wrapped in a
/// listing fragment the same-pathname links are AJAX-intercepted automatically.
/// </summary>
public sealed class QueueTabsVm
{
    /// <summary>Localized aria-label for the tab navigation region.</summary>
    public required string AriaLabel { get; init; }

    /// <summary>Tabs to render, in display order.</summary>
    public required IReadOnlyList<QueueTabVm> Tabs { get; init; }
}

/// <summary>A single status tab with an optional count badge.</summary>
public sealed class QueueTabVm
{
    /// <summary>Localized tab label.</summary>
    public required string Label { get; init; }

    /// <summary>Target URL (full-page GET; listing.js upgrades it where applicable).</summary>
    public required string Url { get; init; }

    /// <summary>
    /// Count badge value. Null hides the badge entirely (ERR3 — when the
    /// counts endpoint fails the tabs degrade to plain links, no broken chrome).
    /// </summary>
    public int? Count { get; init; }

    /// <summary>Whether this tab matches the active filter (renders aria-current="page").</summary>
    public bool Active { get; init; }
}
