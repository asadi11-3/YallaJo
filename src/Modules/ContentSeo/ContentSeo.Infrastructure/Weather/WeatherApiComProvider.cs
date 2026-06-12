namespace ContentSeo.Infrastructure.Weather;

using System.Net;
using System.Text.Json;
using ContentSeo.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class WeatherApiComProvider : IWeatherProvider, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WeatherOptions options;
    private readonly ILogger<WeatherApiComProvider> logger;
    private readonly HttpClient httpClient;

    public WeatherApiComProvider(IOptions<WeatherOptions> options, ILogger<WeatherApiComProvider> logger)
    {
        this.options = options.Value;
        this.logger = logger;
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(NormalizeBaseUrl(this.options.BaseUrl)),
            Timeout = TimeSpan.FromSeconds(Math.Clamp(this.options.TimeoutSeconds, 1, 60)),
        };
    }

    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(options.ApiKey)
        && string.Equals(options.Provider, "weatherapi", StringComparison.OrdinalIgnoreCase);

    public async Task<WeatherSnapshot> FetchAsync(decimal latitude, decimal longitude, CancellationToken ct)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("WeatherAPI.com provider is not configured.");
        }

        // Free plan caps forecast at 3 days; paid plans go higher. Clamp to a
        // sane upper bound but otherwise honour the configured horizon.
        var days = Math.Clamp(options.ForecastDays, 1, 10);
        var path = $"forecast.json?key={Uri.EscapeDataString(options.ApiKey)}&q={latitude},{longitude}&days={days}&aqi=no&alerts=no";

        using var response = await httpClient.GetAsync(path, ct).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogError("WeatherAPI.com rejected the configured API key. Status={StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Weather provider rejected the configured API key.");
        }

        if ((int)response.StatusCode == 429)
        {
            logger.LogWarning("WeatherAPI.com rate limit reached.");
            throw new InvalidOperationException("Weather provider rate limit reached.");
        }

        if ((int)response.StatusCode >= 500)
        {
            logger.LogWarning("WeatherAPI.com server error {StatusCode}; retrying once.", response.StatusCode);
            using var retry = await httpClient.GetAsync(path, ct).ConfigureAwait(false);
            if (!retry.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Weather provider failed with {(int)retry.StatusCode}.");
            }

            return await MapAsync(retry, ct).ConfigureAwait(false);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Weather provider failed with {(int)response.StatusCode}.");
        }

        return await MapAsync(response, ct).ConfigureAwait(false);
    }

    public void Dispose() => httpClient.Dispose();

    private static async Task<WeatherSnapshot> MapAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<WeatherApiComResponse>(stream, JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Weather provider returned an empty payload.");

        var current = payload.Current ?? throw new InvalidOperationException("Weather provider returned no current conditions.");
        var forecastDays = payload.Forecast?.ForecastDays ?? [];
        var daily = forecastDays
            .Select(day => new DailyForecast(
                day.Date,
                day.Day?.MaxTempC ?? 0m,
                day.Day?.MinTempC ?? 0m,
                day.Day?.Condition?.Text ?? "Unknown",
                day.Day?.Condition?.Icon ?? string.Empty,
                (int)Math.Round(day.Day?.AvgHumidity ?? 0m, MidpointRounding.AwayFromZero)))
            .ToList();

        return new WeatherSnapshot(
            current.TempC,
            current.FeelsLikeC,
            current.Humidity,
            current.WindKph,
            current.WindDegree,
            current.Condition?.Text ?? "Unknown",
            current.Condition?.Icon ?? string.Empty,
            current.Uv,
            daily);
    }

    private static string NormalizeBaseUrl(string? value)
    {
        var baseUrl = string.IsNullOrWhiteSpace(value) ? "https://api.weatherapi.com/v1" : value.Trim();
        return baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
    }
}
