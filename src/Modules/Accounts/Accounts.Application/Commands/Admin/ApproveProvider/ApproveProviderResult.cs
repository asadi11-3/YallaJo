namespace Accounts.Application.Commands.Admin.ApproveProvider;

public sealed record ApproveProviderResult(Guid ApplicationId, Guid UserId, DateTime ApprovedAt);
