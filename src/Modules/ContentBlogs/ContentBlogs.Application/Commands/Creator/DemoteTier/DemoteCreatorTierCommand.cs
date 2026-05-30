using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.DemoteTier;

public sealed record DemoteCreatorTierCommand(Guid ProfileId, CreatorTrustTier TargetTier, string Reason) : ICommand;
