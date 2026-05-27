using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetUserDataExport;

public sealed record UserDataExportDto(
    Guid UserId,
    IReadOnlyList<object> Interactions,
    object? Preferences,
    IReadOnlyList<object> ExcludedEntities,
    DateTime ExportedAt);

public sealed record GetUserDataExportQuery : IQuery<UserDataExportDto>;
