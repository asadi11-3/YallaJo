namespace Accounts.Application.Commands.Admin.RejectProvider;

public sealed record RejectProviderResult(
    Guid ApplicationId,
    Guid UserId,
    DateTime RejectedAt,
    DateTime? CoolingPeriodEndsAt);
