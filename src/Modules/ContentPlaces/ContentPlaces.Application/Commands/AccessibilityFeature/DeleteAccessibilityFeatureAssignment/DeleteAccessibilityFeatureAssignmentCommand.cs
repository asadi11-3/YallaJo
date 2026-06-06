using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.DeleteAccessibilityFeatureAssignment;

/// <summary>
/// Admin: remove a single AccessibilityFeature assignment row (place- or business-level).
/// </summary>
public sealed record DeleteAccessibilityFeatureAssignmentCommand(Guid AssignmentId) : ICommand;
