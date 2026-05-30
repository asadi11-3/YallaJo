namespace Social.Application.Queries.Dtos;

/// <summary>Read projection for a single Report.</summary>
public sealed record ReportDto(
    Guid Id,
    Guid ReporterUserId,
    string EntityType,
    Guid EntityId,
    string Reason,
    string Description,
    string Status,
    DateTime SubmittedAt,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAt,
    string? ResolutionAction,
    string? ResolutionNotes);

/// <summary>Paginated list of Reports.</summary>
public sealed record ReportPageDto(
    IReadOnlyList<ReportDto> Items,
    Guid? NextCursor);

/// <summary>Read projection for a ContentModerationLog entry.</summary>
public sealed record ModerationLogDto(
    Guid Id,
    Guid AdminUserId,
    string EntityType,
    Guid EntityId,
    string Action,
    string? Notes,
    DateTime ActionedAt,
    Guid? SourceReportId);

/// <summary>Paginated list of ModerationLog entries.</summary>
public sealed record ModerationLogPageDto(
    IReadOnlyList<ModerationLogDto> Items,
    Guid? NextCursor);
