using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.UpdateApplication;

public sealed record UpdateCreatorApplicationCommand(
    Guid ApplicationId,
    string? Bio,
    List<string>? PortfolioUrls,
    List<string>? SampleWorkUrls,
    List<Guid>? NicheIds,
    List<string>? FreeTags,
    List<Guid>? LanguageIds,
    List<Guid>? PreferredRegionIds,
    Dictionary<string, string>? SocialHandles) : ICommand;
