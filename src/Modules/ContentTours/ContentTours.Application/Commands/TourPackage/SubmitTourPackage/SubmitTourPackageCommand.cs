using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.SubmitTourPackage;

/// <summary>
/// Provider/creator submits a Draft (or previously Rejected) TourPackage for admin review.
/// Transition: Draft|Rejected -> Submitted. Caller must own the package.
/// </summary>
public sealed record SubmitTourPackageCommand(Guid PackageId) : ICommand;
