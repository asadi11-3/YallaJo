using System.Text.Json.Serialization;

namespace ContentSeo.Infrastructure.Weather;

internal sealed record WeatherApiComResponse(
    [property: JsonPropertyName("current")] WeatherApiCurrent? Current,
    [property: JsonPropertyName("forecast")] WeatherApiForecast? Forecast);

internal sealed record WeatherApiCurrent(
    [property: JsonPropertyName("temp_c")] decimal TempC,
    [property: JsonPropertyName("feelslike_c")] decimal FeelsLikeC,
    [property: JsonPropertyName("humidity")] int Humidity,
    [property: JsonPropertyName("wind_kph")] decimal WindKph,
    [property: JsonPropertyName("wind_degree")] int WindDegree,
    [property: JsonPropertyName("uv")] decimal? Uv,
    [property: JsonPropertyName("condition")] WeatherApiCondition? Condition);

internal sealed record WeatherApiForecast(
    [property: JsonPropertyName("forecastday")] IReadOnlyList<WeatherApiForecastDay>? ForecastDays);

internal sealed record WeatherApiForecastDay(
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("day")] WeatherApiDay? Day,
    [property: JsonPropertyName("astro")] WeatherApiAstro? Astro);

internal sealed record WeatherApiDay(
    [property: JsonPropertyName("maxtemp_c")] decimal MaxTempC,
    [property: JsonPropertyName("mintemp_c")] decimal MinTempC,
    [property: JsonPropertyName("avghumidity")] decimal AvgHumidity,
    [property: JsonPropertyName("condition")] WeatherApiCondition? Condition);

internal sealed record WeatherApiAstro(
    [property: JsonPropertyName("sunrise")] string? Sunrise,
    [property: JsonPropertyName("sunset")] string? Sunset);

internal sealed record WeatherApiCondition(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("icon")] string? Icon);
