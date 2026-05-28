using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.CreateGuideDiscount;

/// <summary>Guide creates a discount for their tours.</summary>
public sealed record CreateGuideDiscountCommand(
    Guid? TourId,
    string Name,
    string? Description,
    GuideDiscountType DiscountType,
    decimal DiscountValue,
    string Currency,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount)
    : ICommand<Guid>;
