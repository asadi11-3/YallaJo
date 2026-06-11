using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.LookupAssignableGuides;

/// <summary>
/// [Backend] B7 — typeahead over guides eligible for assignment to a tour (F10).
/// Mirrors the assign command's validation set: active guide profiles not already
/// assigned to the tour. Page size clamped to 1-10.
/// </summary>
public sealed record LookupAssignableGuidesQuery(Guid TourId, string? Term, int PageSize)
    : IQuery<IReadOnlyList<GuideLookupDto>>;

/// <summary>Minimal lookup row for the guide picker combobox.</summary>
public sealed record GuideLookupDto(Guid UserId, string DisplayName, string? AvatarUrl);
