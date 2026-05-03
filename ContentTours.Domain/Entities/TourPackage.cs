using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Domain.Entities;

public sealed class TourPackage : AuditableEntity, IAggregateRoot
{
    private readonly List<TourPackageInclusion> _tourPackageInclusions = [];

    private TourPackage() { } // EF Core

    public Guid TourId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Money Price { get; private set; } = default!;

    // Persisted column kept in sync with Money.Currency on every write
    // (Create/Update). Both are derived from the same normalized input,
    // so they cannot drift. Schema unchanged.
    public string Currency { get; private set; } = string.Empty;

    public int? MaxParticipants { get; private set; }

    public DateTime? ValidFrom { get; private set; }

    public DateTime? ValidTo { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Tour Tour { get; private set; } = default!;

    public IReadOnlyCollection<TourPackageInclusion> TourPackageInclusions =>
        _tourPackageInclusions.AsReadOnly();

    // Factory
    public static TourPackage Create(
        Guid tourId,
        string name,
        string? description,
        decimal priceAmount,
        string currency,
        int? maxParticipants,
        DateTime? validFrom,
        DateTime? validTo)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required", nameof(tourId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (priceAmount <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(priceAmount));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be 3-letter ISO code", nameof(currency));

        if (validTo.HasValue && validFrom.HasValue && validTo < validFrom)
            throw new ArgumentException("ValidTo must be after ValidFrom", nameof(validTo));

        var normalizedCurrency = currency.ToUpperInvariant();

        return new TourPackage
        {
            Id = Guid.CreateVersion7(),
            TourId = tourId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Price = new Money(priceAmount, normalizedCurrency),
            Currency = normalizedCurrency,
            MaxParticipants = maxParticipants,
            ValidFrom = validFrom,
            ValidTo = validTo,
            IsActive = true
        };
    }

    // Update
    public void Update(
        string name,
        string? description,
        decimal priceAmount,
        string currency,
        int? maxParticipants,
        DateTime? validTo)
    {
        EnsureNotDeleted();

        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive package");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (priceAmount <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(priceAmount));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be 3-letter ISO code", nameof(currency));

        if (validTo.HasValue && ValidFrom.HasValue && validTo < ValidFrom)
            throw new ArgumentException("ValidTo must be after ValidFrom", nameof(validTo));

        var normalizedCurrency = currency.ToUpperInvariant();

        Name = name.Trim();
        Description = description?.Trim();
        Price = new Money(priceAmount, normalizedCurrency);
        Currency = normalizedCurrency;
        MaxParticipants = maxParticipants;
        ValidTo = validTo;

        MarkUpdated();
    }

    // Soft delete
    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
            return;

        IsActive = false;
        MarkUpdated();
    }

    // Add inclusion
    public void AddInclusion(TourPackageInclusion inclusion)
    {
        EnsureNotDeleted();

        if (inclusion is null)
            throw new ArgumentNullException(nameof(inclusion));

        // Deduplicate by normalized description (case-insensitive),
        // not by reference — a freshly-created entity never matches by reference.
        var normalized = inclusion.Description?.Trim() ?? string.Empty;

        if (_tourPackageInclusions.Any(x =>
                string.Equals(x.Description, normalized, StringComparison.OrdinalIgnoreCase)))
            return;

        _tourPackageInclusions.Add(inclusion);
        MarkUpdated();
    }

    // Remove inclusion
    public void RemoveInclusion(Guid inclusionId)
    {
        EnsureNotDeleted();

        var inclusion = _tourPackageInclusions
            .FirstOrDefault(x => x.Id == inclusionId);

        if (inclusion is null)
            throw new InvalidOperationException("Inclusion not found");

        _tourPackageInclusions.Remove(inclusion);
        MarkUpdated();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException("TourPackage is deleted");
    }
}
