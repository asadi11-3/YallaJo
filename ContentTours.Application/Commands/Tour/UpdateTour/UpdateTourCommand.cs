using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.UpdateTour;

public sealed record UpdateTourCommand(
    Guid Id,
    byte[] RowVersion,
    string Name,
    string Slug,
    Difficulty Difficulty,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    decimal Latitude,
    decimal Longitude,
    string? Description = null,
    string? ShortDescription = null,
    int? MinAge = null,
    decimal? MeetingPointLatitude = null,
    decimal? MeetingPointLongitude = null,
    Guid PlaceId = default,
    bool IsChildFriendly = false,
    bool IsAccessible = false,
    int? AgeRestriction = null,
    bool IsInstantBooking = false,
    int CancellationPolicyHours = 24,
    string? MetaTitle = null,
    string? MetaDescription = null) : ICommand;
