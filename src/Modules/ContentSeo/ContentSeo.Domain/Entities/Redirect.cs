using ContentSeo.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class Redirect : AuditableEntity, IAggregateRoot
{
    private static readonly int[] AllowedStatusCodes = [301, 302];

    private Redirect() { } // EF Core

    public string OldUrl { get; private set; } = string.Empty;
    public string NewUrl { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int HitCount { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Redirect Create(string oldUrl, string newUrl, int statusCode)
    {
        if (string.IsNullOrWhiteSpace(oldUrl))
            throw new ArgumentException("OldUrl is required.", nameof(oldUrl));
        if (string.IsNullOrWhiteSpace(newUrl))
            throw new ArgumentException("NewUrl is required.", nameof(newUrl));
        if (!AllowedStatusCodes.Contains(statusCode))
            throw new ArgumentException(
                $"StatusCode must be one of: {string.Join(", ", AllowedStatusCodes)}.",
                nameof(statusCode));

        var redirect = new Redirect
        {
            Id         = Guid.CreateVersion7(),
            OldUrl     = oldUrl.Trim(),
            NewUrl     = newUrl.Trim(),
            StatusCode = statusCode,
            IsActive   = true,
            HitCount   = 0,
        };

        redirect.AddDomainEvent(new RedirectCreatedDomainEvent(
            redirect.Id, redirect.OldUrl, redirect.NewUrl, redirect.StatusCode));

        return redirect;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    public void Activate()
    {
        EnsureNotDeleted();
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        IsActive = false;
        MarkUpdated();
        AddDomainEvent(new RedirectDeactivatedDomainEvent(Id, OldUrl));
    }

    public void Update(string? newUrl, int? statusCode)
    {
        EnsureNotDeleted();

        var changed = false;
        var originalNewUrl = NewUrl;
        var originalStatusCode = StatusCode;

        if (!string.IsNullOrWhiteSpace(newUrl) && !string.Equals(NewUrl, newUrl.Trim(), StringComparison.Ordinal))
        {
            NewUrl = newUrl.Trim();
            changed = true;
        }

        if (statusCode.HasValue && StatusCode != statusCode.Value)
        {
            if (!AllowedStatusCodes.Contains(statusCode.Value))
                throw new ArgumentException(
                    $"StatusCode must be one of: {string.Join(", ", AllowedStatusCodes)}.",
                    nameof(statusCode));

            StatusCode = statusCode.Value;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        MarkUpdated();
        AddDomainEvent(new RedirectUpdatedDomainEvent(Id, OldUrl, originalNewUrl, NewUrl, originalStatusCode, StatusCode));
    }

    /// <summary>
    /// Records a single hit on this redirect. Fire-and-forget — no domain event, no audit stamp.
    /// </summary>
    public void IncrementHit()
    {
        HitCount++;
    }

    /// <summary>
    /// Rewrites the target URL after a chain-flattening pass (A → B → C becomes A → C).
    /// </summary>
    public void RewriteTo(string flattenedNewUrl, int hopsCollapsed)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(flattenedNewUrl))
            throw new ArgumentException("FlattenedNewUrl is required.", nameof(flattenedNewUrl));
        if (hopsCollapsed < 1)
            throw new ArgumentOutOfRangeException(
                nameof(hopsCollapsed), "HopsCollapsed must be at least 1.");

        var originalNewUrl = NewUrl;  // capture BEFORE mutation
        NewUrl = flattenedNewUrl.Trim();
        MarkUpdated();

        AddDomainEvent(new RedirectChainFlattenedDomainEvent(
            Id, OldUrl, originalNewUrl, NewUrl, hopsCollapsed));
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException(
                "Redirect.Deleted: operation not permitted on a soft-deleted redirect.");
    }
}
