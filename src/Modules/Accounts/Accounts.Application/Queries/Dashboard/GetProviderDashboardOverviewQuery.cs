using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Dashboard;

/// <summary>Dashboard overview stats for a provider.</summary>
public sealed record GetProviderDashboardOverviewQuery : IQuery<ProviderDashboardOverviewResult>;

public sealed record ProviderDashboardOverviewResult(
    Guid ApplicationId,
    ProviderType ProviderType,
    ProviderApplicationStatus Status,
    string BusinessName,
    int TotalDocuments,
    int ExpiredDocuments,
    int ExpiringIn30DaysDocuments,
    int PendingActionsCount,
    DateTime? ReviewDeadline,
    bool IsApproved);
