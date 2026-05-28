using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.UpdateGuideDiscount;

/// <summary>Guide updates an existing discount. DiscountType cannot be changed after creation.</summary>
public sealed record UpdateGuideDiscountCommand(
    Guid DiscountId,
    string Name,
    string? Description,
    decimal DiscountValue,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount)
    : ICommand;
