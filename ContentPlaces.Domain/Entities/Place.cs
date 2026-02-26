using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentPlaces.Domain.Entities;

public sealed class Place : AuditableEntity, IAggregateRoot
{
    private readonly List<PlaceTranslation> _placeTranslations = [];

    private Place() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PlaceType PlaceType { get; private set; }
    public Location Location { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsVerified { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyCollection<PlaceTranslation> PlaceTranslations => _placeTranslations.AsReadOnly();
}
