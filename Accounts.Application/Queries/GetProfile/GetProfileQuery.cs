using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetProfile;

public sealed record GetProfileQuery() : IQuery<GetProfileResult>;
