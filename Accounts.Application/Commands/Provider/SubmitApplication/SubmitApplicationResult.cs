namespace Accounts.Application.Commands.Provider.SubmitApplication;

public sealed record SubmitApplicationResult(Guid ApplicationId, DateTime SubmittedAt);
