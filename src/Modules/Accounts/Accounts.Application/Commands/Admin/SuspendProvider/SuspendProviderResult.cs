namespace Accounts.Application.Commands.Admin.SuspendProvider;

public sealed record SuspendProviderResult(Guid ApplicationId, Guid UserId);
