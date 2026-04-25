# YallaJo Codebase: SQL Patterns & Location Configuration Analysis

**Date:** 2025-04-21  
**Scope:** Comprehensive search for raw SQL usage, spatial/geo patterns, Location value object configuration, and CTE/subquery patterns

---

## Executive Summary

The YallaJo codebase demonstrates **disciplined SQL usage** with only **2 instances of raw SQL**:
1. **Haversine distance calculation** (SqlQuery) in PlaceRepository
2. **Atomic insert-if-not-exists** (ExecuteSqlInterpolated) in TranslationCacheRepository

All other database operations use **EF Core LINQ** with bulk operations (ExecuteDeleteAsync, ExecuteUpdateAsync). The **Location value object** is consistently configured across 5 entities with **decimal precision (10,8) for Latitude and (11,8) for Longitude**.

---

## 1. RAW SQL USAGE PATTERNS

### 1.1 SqlQuery Pattern (Haversine Distance Calculation)

**File:** ContentPlaces.Infrastructure/Repositories/PlaceRepository.cs  
**Lines:** 16-58  
**Method:** GetNearbyAsync(double lat, double lng, double radiusKm, int pageSize, CancellationToken ct)

Key Characteristics:
- Parameterized: Uses interpolated string with EF Core parameter translation
- Justified: Haversine formula cannot be expressed in LINQ; requires raw SQL
- Efficient: Executes distance calculation in SQL Server, filters before returning
- Safe: No SQL injection risk (EF Core handles parameter binding)
- Return Type: NearbyPlaceResult (flat projection record)

The query uses the Haversine formula to calculate great-circle distance between two geographic points.

---

### 1.2 ExecuteSqlInterpolated Pattern (Atomic Insert-If-Not-Exists)

**File:** ContentCore.Infrastructure/Repositories/TranslationCacheRepository.cs  
**Lines:** 49-67  
**Method:** TryAddCacheEntryAsync(TranslationCache entry, CancellationToken ct)

Key Characteristics:
- Parameterized: Uses ExecuteSqlInterpolatedAsync with parameter binding
- Justified: Atomic insert-if-not-exists with locking hints (UPDLOCK, HOLDLOCK)
- Concurrency-Safe: Uses SQL Server locking hints to prevent race conditions
- Intentional Bypass: Explicitly bypasses Unit of Work (not staged in SaveChangesAsync)
- Locking Strategy: WITH (UPDLOCK, HOLDLOCK) for serializable insert-or-skip semantics
- Return: Boolean indicating whether row was inserted

---

## 2. SPATIAL/GEO QUERY PATTERNS

### 2.1 Haversine Distance Calculation (Only Geo Pattern)

Location: PlaceRepository.GetNearbyAsync

Algorithm: Haversine formula (great-circle distance)
Earth Radius: 6371 km
Precision: Suitable for distances up to ~1000 km
Performance: Calculated in SQL Server; filters before returning to application
Validation: Checks Latitude <> 0.0 AND Longitude <> 0.0 to exclude unset locations

### 2.2 No Native Spatial Types Used

Finding: The codebase does NOT use SQL Server GEOGRAPHY or GEOMETRY types. Instead:
- Latitude/Longitude stored as decimal columns
- Distance calculations performed using trigonometric functions
- This approach is simpler and avoids spatial index overhead for this use case

---

## 3. LOCATION VALUE OBJECT CONFIGURATION

### 3.1 Location Value Object Definition

File: YallaJo.SharedKernel.Domain/ValueObjects/Location.cs

Key Characteristics:
- Type: Sealed value object (immutable)
- Validation: Constructor validates latitude [-90, 90] and longitude [-180, 180]
- Precision: Uses decimal for storage (higher precision than double)
- Distance Calculation: Implements Haversine formula in C# for in-memory calculations
- Equality: Based on Latitude and Longitude components

---

### 3.2 EF Configuration Pattern for Location (OwnsOne)

All entities using Location follow the same configuration pattern:

builder.OwnsOne(e => e.Location, loc =>
{
    loc.Property(l => l.Latitude).HasColumnName(" Latitude\).HasPrecision(10, 8);
 loc.Property(l => l.Longitude).HasColumnName(\Longitude\).HasPrecision(11, 8);
});

Column Configuration:
- Latitude: decimal(10, 8) — 10 total digits, 8 decimal places
 Range: -90.00000000 to 90.00000000
 Precision: ~1.1 mm at equator
- Longitude: decimal(11, 8) — 11 total digits, 8 decimal places
 Range: -180.00000000 to 180.00000000
 Precision: ~1.1 mm at equator

---

### 3.3 Entities Using Location Value Object

#### 3.3.1 Place Entity
File: ContentPlaces.Infrastructure/Persistence/Configurations/PlaceConfiguration.cs
Lines: 34-38
Table: content_places.Places
Columns: Latitude, Longitude

#### 3.3.2 Business Entity
File: ContentPlaces.Infrastructure/Persistence/Configurations/BusinessConfiguration.cs
Lines: 36-40
Table: content_places.Businesses
Columns: Latitude, Longitude

#### 3.3.3 Tour Entity (with MeetingPoint)
File: ContentTours.Infrastructure/Persistence/Configurations/TourConfiguration.cs
Lines: 52-62
Table: content_tours.Tours
Columns:
- Primary Location: Latitude, Longitude
- Meeting Point: MeetingPointLatitude, MeetingPointLongitude

#### 3.3.4 TourWaypoint Entity
File: ContentTours.Infrastructure/Persistence/Configurations/TourWaypointConfiguration.cs
Lines: 26-30
Table: content_tours.TourWaypoints
Columns: Latitude, Longitude

#### 3.3.5 LocationSnapshot Entity (Tracking Module)
File: Tracking.Infrastructure/Persistence/Configurations/LocationSnapshotConfiguration.cs
Lines: 18-22
Table: tracking.LocationSnapshots
Columns: Latitude, Longitude

---

## 4. PLACE ENTITY EF CONFIGURATION (COMPLETE)

File: ContentPlaces.Infrastructure/Persistence/Configurations/PlaceConfiguration.cs
Lines: 1-134

Key Configuration Elements:
- Table: Places in schema content_places
- Primary Key: Id (Guid, not auto-generated)
- Location: Owned type with Latitude (10,8) and Longitude (11,8)
- Soft Delete: IsDeleted boolean with query filter
- Audit Fields: CreatedAt, UpdatedAt, DeletedAt
- Concurrency: RowVersion (timestamp)
- Rating: AverageRating decimal(3,2), ReviewCount int
- Accessibility: IsWheelchairAccessible, HasAudioGuide, HasBrailleSignage
- SEO: MetaTitle, MetaDescription
- Relationships: PlaceTranslations (cascade delete)

Indexes:
- Slug (unique)
- IsFeatured
- IsVerified
- CategoryId
- TourCount

Query Filter: All queries automatically exclude soft-deleted places

---

## 5. CTE AND SUBQUERY PATTERNS

### 5.1 Subquery Pattern: Insert-If-Not-Exists

File: ContentCore.Infrastructure/Repositories/TranslationCacheRepository.cs
Lines: 54-64

Pattern Type: INSERT...SELECT with NOT EXISTS subquery
Purpose: Atomic insert-if-not-exists to prevent duplicate cache entries
Locking: WITH (UPDLOCK, HOLDLOCK) for serializable semantics

---

### 5.2 No CTEs Found

Finding: The codebase contains NO Common Table Expressions (CTEs) using WITH (...) syntax.

The only WITH clause found is the SQL Server table hint WITH (UPDLOCK, HOLDLOCK) in the TranslationCacheRepository.

---

## 6. BULK OPERATIONS (ExecuteDeleteAsync / ExecuteUpdateAsync)

### 6.1 ExecuteDeleteAsync Usage

The codebase uses bulk delete operations in multiple locations:

#### 6.1.1 OutboxCleaner (Batch Deletion)
File: YallaJo.SharedKernel.Infrastructure/Outbox/OutboxCleaner.cs
Lines: 87-119

Key Characteristics:
- Provider-Aware: Detects database provider and uses appropriate strategy
- Batch Processing: Deletes in configurable batch sizes
- Fallback: Gracefully handles providers that don't support ExecuteDeleteAsync
- Supported Providers: SQL Server, PostgreSQL (Npgsql), MySQL (Pomelo)

#### 6.1.2 AuthCleanupService (Scheduled Cleanup)
File: Auth.Infrastructure/BackgroundJobs/AuthCleanupService.cs
Lines: 43-67

Entities Cleaned:
1. RefreshTokens: Expired or revoked tokens
2. Sessions: Expired or revoked sessions
3. OTPs: Used or expired one-time passwords

#### 6.1.3 BusinessRepository (Bulk Replace)
File: ContentPlaces.Infrastructure/Repositories/BusinessRepository.cs
Lines: 47-54

Pattern: Delete-then-insert for atomic replacement

---

### 6.2 ExecuteUpdateAsync Usage

File: YallaJo.SharedKernel.Infrastructure/Data/Repositories/EfWriteRepository.cs
Lines: 22-28

Usage: Generic bulk update method available to all repositories

---

## 7. SUMMARY TABLE

| Pattern | Count | Files | Purpose |
|---------|-------|-------|---------|
| SqlQuery | 1 | PlaceRepository.cs | Haversine distance calculation |
| ExecuteSqlInterpolated | 1 | TranslationCacheRepository.cs | Atomic insert-if-not-exists |
| ExecuteDeleteAsync | 5+ | OutboxCleaner, AuthCleanupService, BusinessRepository, EfWriteRepository | Bulk deletion |
| ExecuteUpdateAsync | 1 | EfWriteRepository | Bulk update (generic) |
| Location OwnsOne | 5 | Place, Business, Tour, TourWaypoint, LocationSnapshot | Geo-spatial data |
| CTEs (WITH) | 0 | — | Not used |
| Subqueries | 1 | TranslationCacheRepository | Insert-if-not-exists |

---

## 8. RECOMMENDATIONS

### 8.1 Current State: Excellent

✓ Minimal raw SQL: Only 2 instances, both justified
✓ Consistent Location configuration: All entities follow same pattern
✓ Safe parameterization: No SQL injection risks
✓ Efficient bulk operations: Uses ExecuteDeleteAsync/ExecuteUpdateAsync
✓ Provider-aware: Graceful fallbacks for different database providers

### 8.2 Future Considerations

1. Spatial Indexes: If nearby-place queries become a bottleneck, consider:
 - Adding non-clustered indexes on (Latitude, Longitude) in Places table
 - Or migrating to SQL Server GEOGRAPHY type with spatial indexes

2. CTE Usage: If complex hierarchical or recursive queries emerge, CTEs are available but not currently needed

3. Monitoring: Track ExecuteSqlInterpolated usage to ensure it remains minimal and justified

---

## Appendix: File Locations Reference

| Entity/Component | Configuration File | Domain File |
|------------------|-------------------|------------|
| Place | ContentPlaces.Infrastructure/Persistence/Configurations/PlaceConfiguration.cs | ContentPlaces.Domain/Entities/Place.cs |
| Business | ContentPlaces.Infrastructure/Persistence/Configurations/BusinessConfiguration.cs | ContentPlaces.Domain/Entities/Business.cs |
| Tour | ContentTours.Infrastructure/Persistence/Configurations/TourConfiguration.cs | ContentTours.Domain/Entities/Tour.cs |
| TourWaypoint | ContentTours.Infrastructure/Persistence/Configurations/TourWaypointConfiguration.cs | ContentTours.Domain/Entities/TourWaypoint.cs |
| LocationSnapshot | Tracking.Infrastructure/Persistence/Configurations/LocationSnapshotConfiguration.cs | Tracking.Domain/Entities/LocationSnapshot.cs |
| Location (VO) | — | YallaJo.SharedKernel.Domain/ValueObjects/Location.cs |
