using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.CreateApplication;

public sealed record CreateCreatorApplicationCommand(
    string? Bio,
    List<string>? PortfolioUrls,
    List<string>? SampleWorkUrls,
    List<Guid>? NicheIds,
    List<string>? FreeTags,
    List<Guid>? LanguageIds,
    List<Guid>? PreferredRegionIds,
    Dictionary<string, string>? SocialHandles) : ICommand<CreateCreatorApplicationResult>;
