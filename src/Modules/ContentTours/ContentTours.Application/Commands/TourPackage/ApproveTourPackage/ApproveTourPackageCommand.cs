using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.ApproveTourPackage;

/// <summary>
/// Admin approves a Submitted TourPackage. Transition: Submitted -> Approved.
/// Caller must be an admin (gated at the endpoint via .RequireAuthorization("Admin")).
/// </summary>
public sealed record ApproveTourPackageCommand(Guid PackageId) : ICommand;
