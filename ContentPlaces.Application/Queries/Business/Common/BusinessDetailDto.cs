using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Queries.Business.Common
{
    public sealed record BusinessDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string BusinessType,
    Guid? PlaceId,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? City,
    string? Country,
    string? PostalCode,
    string? Phone,
    string? Email,
    string? Website,
    decimal AverageRating,
    int ReviewCount,
    bool IsVerified,
    bool IsFeatured,
    Guid OwnerId,
    string Status,
    string? RejectionReason,
    DateTime CreatedAt);
}
