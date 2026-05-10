using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Domain.Entities;

public sealed class TourPackage : AuditableEntity, IAggregateRoot
{
    private readonly List<TourPackageTour> _includedTours = [];
    private readonly List<TourPackageInclusion> _inclusions = [];

    private TourPackage() { } // EF Core

    public Guid CreatedByUserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Money Price { get; private set; } = default!;

    public string Currency { get; private set; } = string.Empty;

    public int? MaxParticipants { get; private set; }

    public DateTime? ValidFrom { get; private set; }

    public DateTime? ValidTo { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<TourPackageTour> IncludedTours => _includedTours.AsReadOnly();

    public IReadOnlyCollection<TourPackageInclusion> Inclusions => _inclusions.AsReadOnly();

    public static TourPackage Create(
        string name,
        string? description,
        Money price,
        string currency,
        int? maxParticipants,
        DateTime? validFrom,
        DateTime? validTo,
        Guid createdByUserId,
        IReadOnlyCollection<Guid> includedTourIds,
        IReadOnlyCollection<string> inclusionDescriptions)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(price);
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));
        ArgumentNullException.ThrowIfNull(includedTourIds);
        ArgumentNullException.ThrowIfNull(inclusionDescriptions);

        var distinctTourIds = includedTourIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctTourIds.Count < 2)
        {
            // Defence-in-depth — the handler must surface
            // TourPackage.InsufficientInclusions / TourPackage.DuplicateInclusions BEFORE
            // calling Create. Reaching this branch indicates a programming error.
            throw new ArgumentException(
                "A tour package must include at least 2 distinct tours.",
                nameof(includedTourIds));
        }

        if (validFrom.HasValue && validTo.HasValue && validFrom.Value >= validTo.Value)
        {
            throw new ArgumentException(
                "ValidFrom must be earlier than ValidTo.",
                nameof(validFrom));
        }

        var normalizedCurrency = currency.ToUpperInvariant();

        var package = new TourPackage
        {
            // Id auto-assigned by AuditableEntity ctor (Guid.CreateVersion7()).
            CreatedByUserId = createdByUserId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Price = new Money(price.Amount, normalizedCurrency),
            Currency = normalizedCurrency,
            MaxParticipants = maxParticipants,
            ValidFrom = validFrom,
            ValidTo = validTo,
            IsActive = true,
        };

        foreach (var tourId in distinctTourIds)
        {
            package._includedTours.Add(TourPackageTour.Create(package.Id, tourId));
        }

        var sortOrder = 1;
        foreach (var description1 in inclusionDescriptions)
        {
            if (string.IsNullOrWhiteSpace(description1))
                continue;

            var trimmed = description1.Trim();
            var alreadyAdded = package._inclusions.Any(i =>
                string.Equals(i.Description, trimmed, StringComparison.OrdinalIgnoreCase));
            if (alreadyAdded)
                continue;

            package._inclusions.Add(TourPackageInclusion.Create(package.Id, trimmed, sortOrder));
            sortOrder++;
        }

        return package;
    }

    public void Update(
        string name,
        string? description,
        Money price,
        string currency,
        int? maxParticipants,
        DateTime? validTo,
        IReadOnlyCollection<Guid> includedTourIds)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(price);
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        ArgumentNullException.ThrowIfNull(includedTourIds);

        var distinctTourIds = includedTourIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctTourIds.Count < 2)
        {
            throw new ArgumentException(
                "A tour package must include at least 2 distinct tours.",
                nameof(includedTourIds));
        }

        if (ValidFrom.HasValue && validTo.HasValue && ValidFrom.Value >= validTo.Value)
        {
            throw new ArgumentException(
                "ValidTo must be after the existing ValidFrom.",
                nameof(validTo));
        }

        var normalizedCurrency = currency.ToUpperInvariant();

        Name = name.Trim();
        Description = description?.Trim();
        Price = new Money(price.Amount, normalizedCurrency);
        Currency = normalizedCurrency;
        MaxParticipants = maxParticipants;
        ValidTo = validTo;

        var existingIds = _includedTours.Select(x => x.TourId).ToHashSet();
        var newIds = distinctTourIds.ToHashSet();

        _includedTours.RemoveAll(link => !newIds.Contains(link.TourId));

        // Add links new to this update.
        foreach (var tourId in distinctTourIds.Where(id => !existingIds.Contains(id)))
        {
            _includedTours.Add(TourPackageTour.Create(Id, tourId));
        }

        MarkUpdated();
    }

    public TourPackageInclusion AddInclusion(string description)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        var trimmed = description.Trim();

        var nextSortOrder = _inclusions.Count == 0
            ? 1
            : _inclusions.Max(i => i.SortOrder) + 1;

        var inclusion = TourPackageInclusion.Create(Id, trimmed, nextSortOrder);
        _inclusions.Add(inclusion);
        MarkUpdated();
        return inclusion;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "TourPackage.Deleted: operation not permitted on a soft-deleted package.");
        }
    }
}
