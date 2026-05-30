using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.SubmitApplication;

public sealed record SubmitApplicationCommand : ICommand<SubmitApplicationResult>;
