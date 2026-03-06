using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Azure Cognitive Services Translator API implementation.
/// Uses the REST API directly via HttpClient — no Azure SDK dependency.
/// Docs: https://learn.microsoft.com/en-us/azure/ai-services/translator/reference/v3-0-translate
/// </summary>
public sealed class AzureTranslateService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureTranslateService> _logger;
    private readonly string _subscriptionKey;
    private readonly string _region;
    private readonly string _endpoint;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AzureTranslateService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AzureTranslateService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _subscriptionKey = configuration["AzureTranslator:SubscriptionKey"]
            ?? throw new InvalidOperationException("AzureTranslator:SubscriptionKey is not configured.");
        _region = configuration["AzureTranslator:Region"]
            ?? throw new InvalidOperationException("AzureTranslator:Region is not configured.");
        _endpoint = configuration["AzureTranslator:Endpoint"]
            ?? "https://api.cognitive.microsofttranslator.com";
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default)
    {
        var results = await BatchTranslateAsync([text], fromLanguageCode, toLanguageCode, ct);
        return results[0];
    }

    public async Task<IReadOnlyList<TranslationResult>> BatchTranslateAsync(
        IReadOnlyList<string> texts,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default)
    {
        var route = $"/translate?api-version=3.0&from={fromLanguageCode}&to={toLanguageCode}";
        var body = texts.Select(t => new { Text = t }).ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint + route);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
        request.Headers.Add("Ocp-Apim-Subscription-Region", _region);

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Azure Translator API error: {StatusCode} — {Body}",
                response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Azure Translator API returned {(int)response.StatusCode}: {errorBody}");
        }

        var azureResults = await response.Content.ReadFromJsonAsync<AzureTranslateResponse[]>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Azure Translator API returned null response.");

        var results = new List<TranslationResult>(azureResults.Length);
        for (var i = 0; i < azureResults.Length; i++)
        {
            var translation = azureResults[i].Translations?.FirstOrDefault();
            results.Add(new TranslationResult(
                OriginalText: texts[i],
                TranslatedText: translation?.Text ?? texts[i],
                FromLanguage: fromLanguageCode,
                ToLanguage: translation?.To ?? toLanguageCode,
                Confidence: azureResults[i].DetectedLanguage?.Score));
        }

        return results;
    }

    public async Task<string> DetectLanguageAsync(string text, CancellationToken ct = default)
    {
        var route = "/detect?api-version=3.0";
        var body = new[] { new { Text = text } };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint + route);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
        request.Headers.Add("Ocp-Apim-Subscription-Region", _region);

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<AzureDetectResponse[]>(JsonOptions, ct);
        return results?.FirstOrDefault()?.Language ?? "en";
    }

    public async Task<IReadOnlyList<SupportedLanguage>> GetSupportedLanguagesAsync(CancellationToken ct = default)
    {
        var route = "/languages?api-version=3.0&scope=translation";

        using var request = new HttpRequestMessage(HttpMethod.Get, _endpoint + route);
        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureLanguagesResponse>(JsonOptions, ct);
        if (result?.Translation is null)
            return Array.Empty<SupportedLanguage>();

        return result.Translation
            .Select(kvp => new SupportedLanguage(
                Code: kvp.Key,
                Name: kvp.Value.Name,
                NativeName: kvp.Value.NativeName))
            .OrderBy(l => l.Code)
            .ToList();
    }

    // ── Azure API response DTOs ────────────────────────────────────────────

    private sealed class AzureTranslateResponse
    {
        public AzureDetectedLanguage? DetectedLanguage { get; set; }
        public AzureTranslation[]? Translations { get; set; }
    }

    private sealed class AzureDetectedLanguage
    {
        public string Language { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    private sealed class AzureTranslation
    {
        public string Text { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }

    private sealed class AzureDetectResponse
    {
        public string Language { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    private sealed class AzureLanguagesResponse
    {
        public Dictionary<string, AzureLanguageInfo>? Translation { get; set; }
    }

    private sealed class AzureLanguageInfo
    {
        public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
        public string Dir { get; set; } = string.Empty;
    }
}
