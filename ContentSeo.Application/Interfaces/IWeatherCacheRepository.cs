using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Repository for the <see cref="WeatherCache"/> aggregate root.
/// Compile-only stub for Wave-4 pre-work — by-place / expiry-cleanup
/// query methods will be added during TASK 4 implementation.
/// </summary>
public interface IWeatherCacheRepository : IRepository<WeatherCache>;
