using MediatR;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.SubmitReport;

/// <summary>Submits a moderation report against an entity.</summary>
public sealed record SubmitReportCommand(
    Guid ReporterUserId,
    ReportableEntityType EntityType,
    Guid EntityId,
    ReportReason Reason,
    string Description) : IRequest<Result<Guid>>;
