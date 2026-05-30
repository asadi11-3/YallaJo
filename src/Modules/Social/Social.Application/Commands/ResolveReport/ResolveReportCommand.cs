using MediatR;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.ResolveReport;

/// <summary>Admin resolves an open report and optionally takes action on the underlying entity.</summary>
public sealed record ResolveReportCommand(
    Guid AdminUserId,
    Guid ReportId,
    ModerationAction Action,
    string? Notes = null) : IRequest<Result>;
