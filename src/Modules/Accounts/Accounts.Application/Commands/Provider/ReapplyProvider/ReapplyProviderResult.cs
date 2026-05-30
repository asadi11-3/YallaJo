namespace Accounts.Application.Commands.Provider.ReapplyProvider;

public sealed record ReapplyProviderResult(Guid ApplicationId, DateTime ReappliedAt);
