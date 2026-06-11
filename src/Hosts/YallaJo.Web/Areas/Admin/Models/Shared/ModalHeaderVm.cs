namespace YallaJo.Web.Areas.Admin.Models.Shared;

/// <summary>
/// View model for the shared <c>Partials/_ModalHeader</c> partial (MOD3, A11Y5).
/// <see cref="TitleId"/> must match the modal's <c>aria-labelledby</c> attribute.
/// </summary>
public sealed class ModalHeaderVm
{
    public required string TitleId { get; init; }

    public required string Title { get; init; }

    /// <summary>Pre-localized accessible label for the close (×) button.</summary>
    public required string CloseLabel { get; init; }

    /// <summary>Renders the destructive variant (<c>bg-danger text-white</c> + white close icon).</summary>
    public bool Danger { get; init; }

    /// <summary>
    /// When set, the close control renders as a plain anchor to this URL instead of a
    /// <c>data-bs-dismiss</c> button — used by server-side-opened deep-link modals (PE1)
    /// where no JS is available to dismiss the dialog.
    /// </summary>
    public string? CloseUrl { get; init; }
}
