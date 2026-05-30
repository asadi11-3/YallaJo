using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.DeactivateGuideDiscount;

/// <summary>Guide deactivates (soft-removes) a discount.</summary>
public sealed record DeactivateGuideDiscountCommand(Guid DiscountId) : ICommand;
