using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Application.Commands.Weather.PurgeWeatherCache;

public sealed record PurgeWeatherCacheCommand(Guid Id) : ICommand;
