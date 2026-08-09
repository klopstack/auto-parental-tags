using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoParentalTags.Services;

/// <summary>
/// Service for interacting with the Google Gemini API.
/// </summary>
public class GeminiService : IAiService, IDisposable
{
    private static readonly HashSet<string> ValidAudienceTags =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "kids",
            "teens",
            "adults"
        };

    private static readonly char[] AudienceResponseSeparators =
    {
        ' ',
        '\t',
        '\r',
        '\n',
        '.',
        ',',
        ':',
        ';',
        '-',
        '_',
        '/',
        '\\',
        '(',
        ')',
        '[',
        ']'
    };

    private readonly ILogger<GeminiService> _logger;
    private readonly HttpClient _httpClient;

    private string? _apiKey;
    private string _modelName = "gemini-pro";

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiService"/> class.
    /// </summary>
    /// <param name="logger">
    /// Instance of the <see cref="ILogger{GeminiService}"/> interface.
    /// </param>
    public GeminiService(ILogger<GeminiService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Sanitizes a string for logging to prevent log-forging attacks.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>A sanitized string safe for logging.</returns>
    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Normalizes the media type supplied by the library monitor.
    /// </summary>
    /// <param name="mediaType">The supplied media type.</param>
    /// <returns>Either movie or TV series.</returns>
    private static string NormalizeMediaType(string? mediaType)
    {
        if (string.Equals(
                mediaType,
                "TV series",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                mediaType,
                "series",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                mediaType,
                "tv",
                StringComparison.OrdinalIgnoreCase))
        {
            return "TV series";
        }

        return "movie";
    }

    /// <inheritdoc />
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogDebug(
                "Gemini API key is not configured.");
        }
        else
        {
            _logger.LogDebug(
                "Gemini API key is configured.");
        }
    }

    /// <inheritdoc />
    public void SetEndpoint(string endpoint)
    {
        // Gemini uses a fixed Google API endpoint.
    }

    /// <inheritdoc />
    public void SetModelName(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return;
        }

        _modelName = modelName;

        _logger.LogDebug(
            "Gemini model name set to: {ModelName}",
            SanitizeForLog(modelName));
    }

    /// <inheritdoc />
    public async Task<string?> DetermineTargetAudienceAsync(
        string mediaType,
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning(
                "Gemini API key is not configured");

            return null;
        }

        var normalizedMediaType =
            NormalizeMediaType(mediaType);

        try
        {
            var prompt = BuildPrompt(
                normalizedMediaType,
                title,
                year,
                overview,
                officialRating,
                genres);

            _logger.LogDebug(
                "Requesting Gemini audience classification for {MediaType} '{Title}' ({Year})",
                normalizedMediaType,
                SanitizeForLog(title),
                year);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 16
                }
            };

            var jsonContent =
                JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(
                jsonContent,
                Encoding.UTF8,
                "application/json");

            var escapedModelName =
                Uri.EscapeDataString(_modelName);

            var escapedApiKey =
                Uri.EscapeDataString(_apiKey);

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{escapedModelName}:generateContent?key={escapedApiKey}";

            using var response =
                await _httpClient
                    .PostAsync(url, content)
                    .ConfigureAwait(false);

            var responseContent =
                await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Gemini API error for {MediaType} '{Title}': {StatusCode} - {Error}",
                    normalizedMediaType,
                    SanitizeForLog(title),
                    response.StatusCode,
                    responseContent);

                return null;
            }

            using var responseJson =
                JsonDocument.Parse(responseContent);

            if (!responseJson.RootElement.TryGetProperty(
                    "candidates",
                    out var candidates)
                || candidates.ValueKind != JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning(
                    "Gemini returned no candidates for {MediaType} '{Title}'",
                    normalizedMediaType,
                    SanitizeForLog(title));

                return null;
            }

            var firstCandidate =
                candidates[0];

            if (!firstCandidate.TryGetProperty(
                    "content",
                    out var candidateContent)
                || !candidateContent.TryGetProperty(
                    "parts",
                    out var parts)
                || parts.ValueKind != JsonValueKind.Array
                || parts.GetArrayLength() == 0)
            {
                _logger.LogWarning(
                    "Gemini returned no response parts for {MediaType} '{Title}'",
                    normalizedMediaType,
                    SanitizeForLog(title));

                return null;
            }

            var firstPart =
                parts[0];

            if (!firstPart.TryGetProperty(
                    "text",
                    out var textElement))
            {
                _logger.LogWarning(
                    "Gemini returned no text for {MediaType} '{Title}'",
                    normalizedMediaType,
                    SanitizeForLog(title));

                return null;
            }

            var responseText =
                textElement.GetString();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning(
                    "Gemini returned empty text for {MediaType} '{Title}'",
                    normalizedMediaType,
                    SanitizeForLog(title));

                return null;
            }

            var tag =
                ParseAudienceTag(responseText);

            if (tag == null)
            {
                _logger.LogWarning(
                    "Gemini returned an unsupported audience response for {MediaType} '{Title}': {Response}",
                    normalizedMediaType,
                    SanitizeForLog(title),
                    SanitizeForLog(responseText));

                return null;
            }

            _logger.LogInformation(
                "Classified {MediaType} '{Title}' ({Year}) as '{Tag}'",
                normalizedMediaType,
                SanitizeForLog(title),
                year,
                tag);

            return tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error calling Gemini API for {MediaType} '{Title}': {Message}",
                normalizedMediaType,
                SanitizeForLog(title),
                ex.Message);

            return null;
        }
    }

    /// <summary>
    /// Builds the audience-classification prompt.
    /// </summary>
    /// <param name="mediaType">Movie or TV series.</param>
    /// <param name="title">Item title.</param>
    /// <param name="year">Release or premiere year.</param>
    /// <param name="overview">Item synopsis.</param>
    /// <param name="officialRating">Official content rating.</param>
    /// <param name="genres">Item genres.</param>
    /// <returns>The completed prompt.</returns>
    private static string BuildPrompt(
        string mediaType,
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres)
    {
        var informationHeading =
            mediaType.Equals(
                "TV series",
                StringComparison.OrdinalIgnoreCase)
                ? "TV Series Information"
                : "Movie Information";

        var mediaSpecificGuidance =
            mediaType.Equals(
                "TV series",
                StringComparison.OrdinalIgnoreCase)
                ? @"Classify the intended audience of the TV series as a whole.
Do not classify individual seasons or episodes.
Consider the overall premise, long-term themes, marketing, content rating, and intended demographic."
                : @"Classify the intended audience of the movie as a whole.
Consider the film's marketing, themes, content rating, and intended demographic.";

        return $@"Analyze this {mediaType} and determine its PRIMARY TARGET AUDIENCE, not merely its content rating.

Target audience differs from content appropriateness:
- A family-safe title may still primarily target adults.
- A PG-13 action title may primarily target teenagers.
- An unrated animated special may clearly target children.

{mediaSpecificGuidance}

{informationHeading}:
Title: {title}
Year: {year?.ToString(CultureInfo.InvariantCulture) ?? "Unknown"}
Official Rating: {officialRating ?? "Not Rated"}
Genres: {(genres?.Length > 0 ? string.Join(", ", genres) : "Unknown")}
Overview: {overview ?? "No overview available"}

Choose exactly one category:
- kids: primarily targeted at children, generally ages 2-11
- teens: primarily targeted at teenagers, generally ages 12-17
- adults: primarily targeted at adults, generally ages 18+

Consider:
1. Marketing and intended demographic
2. Themes and subject-matter complexity
3. Historical and cultural context
4. Franchise or brand audience
5. Storytelling sophistication
6. For TV series, the intended audience of the overall series

Reply with exactly one lowercase word:
kids
teens
adults";
    }

    /// <summary>
    /// Parses a supported audience tag from the Gemini response.
    /// </summary>
    /// <param name="response">The Gemini response.</param>
    /// <returns>A supported audience tag, or null.</returns>
    private static string? ParseAudienceTag(string response)
    {
        var normalized = response
            .Trim()
            .ToLower(CultureInfo.InvariantCulture);

        if (ValidAudienceTags.Contains(normalized))
        {
            return normalized;
        }

        var words = normalized.Split(
            AudienceResponseSeparators,
            StringSplitOptions.RemoveEmptyEntries);

        if (words.Contains(
                "kids",
                StringComparer.OrdinalIgnoreCase)
            || words.Contains(
                "children",
                StringComparer.OrdinalIgnoreCase)
            || words.Contains(
                "child",
                StringComparer.OrdinalIgnoreCase))
        {
            return "kids";
        }

        if (words.Contains(
                "teens",
                StringComparer.OrdinalIgnoreCase)
            || words.Contains(
                "teen",
                StringComparer.OrdinalIgnoreCase)
            || words.Contains(
                "teenagers",
                StringComparer.OrdinalIgnoreCase)
            || words.Contains(
                "teenager",
                StringComparer.OrdinalIgnoreCase))
        {
            return "teens";
        }

        if (words.Contains(
                "adults",
                StringComparer.OrdinalIgnoreCase)
            || words.Contains(
                "adult",
                StringComparer.OrdinalIgnoreCase)
            || words.Contains(
                "mature",
                StringComparer.OrdinalIgnoreCase))
        {
            return "adults";
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<string[]> GetAvailableModelsAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning(
                "Gemini API key is not configured");

            return Array.Empty<string>();
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://generativelanguage.googleapis.com/v1beta/models");

            request.Headers.Add(
                "x-goog-api-key",
                _apiKey);

            using var response =
                await _httpClient
                    .SendAsync(request)
                    .ConfigureAwait(false);

            var responseContent =
                await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to fetch Gemini models: {StatusCode} - {Error}",
                    response.StatusCode,
                    responseContent);

                return Array.Empty<string>();
            }

            using var responseJson =
                JsonDocument.Parse(responseContent);

            if (!responseJson.RootElement.TryGetProperty(
                    "models",
                    out var modelsArray)
                || modelsArray.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var models = modelsArray
                .EnumerateArray()
                .Where(
                    model =>
                        model.TryGetProperty(
                            "name",
                            out var nameElement)
                        && !string.IsNullOrWhiteSpace(
                            nameElement.GetString())
                        && model.TryGetProperty(
                            "supportedGenerationMethods",
                            out var methods)
                        && methods.ValueKind == JsonValueKind.Array
                        && methods
                            .EnumerateArray()
                            .Any(
                                method =>
                                    string.Equals(
                                        method.GetString(),
                                        "generateContent",
                                        StringComparison.OrdinalIgnoreCase)))
                .Select(
                    model =>
                    {
                        var fullName =
                            model.GetProperty("name").GetString()!;

                        const string prefix =
                            "models/";

                        return fullName.StartsWith(
                            prefix,
                            StringComparison.OrdinalIgnoreCase)
                            ? fullName[prefix.Length..]
                            : fullName;
                    })
                .Where(
                    modelName =>
                        modelName.Contains(
                            "gemini",
                            StringComparison.OrdinalIgnoreCase)
                        && !modelName.Contains(
                            "embedding",
                            StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    modelName => modelName,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _logger.LogDebug(
                "Found {Count} Gemini models",
                models.Length);

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching Gemini models: {Message}",
                ex.Message);

            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the service.
    /// </summary>
    /// <param name="disposing">
    /// Whether managed resources should be disposed.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient.Dispose();
        }
    }
}
