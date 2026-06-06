using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.RejectTourPackage;

/// <summary>
/// Admin rejects a Submitted TourPackage with a reason. Transition: Submitted -> Rejected.
/// Caller must be an admin (gated at the endpoint via .RequireAuthorization("Admin")).
/// </summary>
public sealed record RejectTourPackageCommand(Guid PackageId, string Reason) : ICommand;
