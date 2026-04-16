using Microsoft.EntityFrameworkCore;
using ContentPlaces.Domain.Entities;

namespace ContentPlaces.Application.Interfaces;

public interface IContentPlacesDbContext
{
    DbSet<Business> Businesses { get; }
    DbSet<BusinessAmenity> BusinessAmenities { get; }
    DbSet<BusinessStaff> BusinessStaff { get; }
    DbSet<AccessibilityFeature> AccessibilityFeatures { get; }
    DbSet<Place> Places { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
