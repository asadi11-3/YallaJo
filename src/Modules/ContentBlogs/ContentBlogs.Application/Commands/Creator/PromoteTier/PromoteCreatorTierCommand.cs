using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.PromoteTier;

public sealed record PromoteCreatorTierCommand(Guid ProfileId, CreatorTrustTier TargetTier) : ICommand;
