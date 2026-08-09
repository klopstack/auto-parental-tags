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
/// Service for interacting with OpenAI-compatible APIs such as OpenAI and LocalAI.
/// </summary>
public class OpenAiService : IAiService, IDisposable
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

    private readonly ILogger<OpenAiService> _logger;
    private readonly HttpClient _httpClient;

    private string? _apiKey;
    private string _endpoint =
        "https://api.openai.com/v1/chat/completions";

    private string _modelName = "gpt-3.5-turbo";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiService"/> class.
    /// </summary>
    /// <param name="logger">
    /// Instance of the <see cref="ILogger{OpenAiService}"/> interface.
    /// </param>
    public OpenAiService(ILogger<OpenAiService> logger)
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
    /// Normalizes the supplied media-type label.
    /// </summary>
    /// <param name="mediaType">The supplied media type.</param>
    /// <returns>A normalized media-type label.</returns>
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

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogDebug(
                "OpenAI-compatible API key is not configured or is empty.");
        }
        else
        {
            _logger.LogDebug(
                "OpenAI-compatible API key is configured.");
        }
    }

    /// <inheritdoc />
    public void SetEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        // Accept:
        // - a server base URL
        // - a /v1 base URL
        // - a complete /chat/completions endpoint
        _endpoint = endpoint
            .Trim()
            .TrimEnd('/');

        if (_endpoint.EndsWith(
                "/v1/chat/completions",
                StringComparison.OrdinalIgnoreCase)
            || _endpoint.EndsWith(
                "/chat/completions",
                StringComparison.OrdinalIgnoreCase))
        {
            // Complete endpoint was supplied.
        }
        else if (_endpoint.EndsWith(
                     "/v1",
                     StringComparison.OrdinalIgnoreCase))
        {
            _endpoint += "/chat/completions";
        }
        else
        {
            _endpoint += "/v1/chat/completions";
        }

        _logger.LogInformation(
            "OpenAI endpoint configured: {Endpoint}",
            SanitizeForLog(_endpoint));
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
            "OpenAI-compatible model name set to: {ModelName}",
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
                "Requesting audience classification for {MediaType} '{Title}' ({Year})",
                normalizedMediaType,
                SanitizeForLog(title),
                year);

            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content =
                            "You classify the primary target audience for movies and TV series. "
                            + "Reply with exactly one lowercase word: kids, teens, or adults. "
                            + "Do not explain your answer."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.1,
                max_tokens = 64,
                reasoning_effort = "none",
                metadata = new Dictionary<string, string>
                {
                    ["enable_thinking"] = "false"
                }
            };

            var jsonContent =
                JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(
                jsonContent,
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                _endpoint)
            {
                Content = content
            };

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        _apiKey);
            }

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
                    "AI API error for {MediaType} '{Title}': {StatusCode} - {Error}",
                    normalizedMediaType,
                    SanitizeForLog(title),
                    response.StatusCode,
                    responseContent);

                return null;
            }

            using var responseJson =
                JsonDocument.Parse(responseContent);

            if (!responseJson.RootElement.TryGetProperty(
                    "choices",
                    out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                _logger.LogWarning(
                    "AI API returned no choices for {MediaType} '{Title}'",
                    normalizedMediaType,
                    SanitizeForLog(title));

                return null;
            }

            var firstChoice = choices[0];

            if (!firstChoice.TryGetProperty(
                    "message",
                    out var message)
                || !message.TryGetProperty(
                    "content",
                    out var contentElement))
            {
                _logger.LogWarning(
                    "AI API returned no message content for {MediaType} '{Title}'",
                    normalizedMediaType,
                    SanitizeForLog(title));

                return null;
            }

            var responseText =
                contentElement.GetString();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning(
                    "AI API returned empty content for {MediaType} '{Title}'",
                    normalizedMediaType,
                    SanitizeForLog(title));

                return null;
            }

            var tag =
                ParseAudienceTag(responseText);

            if (tag == null)
            {
                _logger.LogWarning(
                    "AI API returned an unsupported audience response for {MediaType} '{Title}': {Response}",
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
                "Error calling AI API for {MediaType} '{Title}': {Message}",
                normalizedMediaType,
                SanitizeForLog(title),
                ex.Message);

            return null;
        }
    }

    /// <summary>
    /// Builds the classification prompt.
    /// </summary>
    /// <param name="mediaType">Movie or TV series.</param>
    /// <param name="title">Item title.</param>
    /// <param name="year">Release or premiere year.</param>
    /// <param name="overview">Item synopsis.</param>
    /// <param name="officialRating">Official content rating.</param>
    /// <param name="genres">Item genres.</param>
    /// <returns>The completed classification prompt.</returns>
    private static string BuildPrompt(
        string mediaType,
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres)
    {
        var typeHeading =
            mediaType.Equals(
                "TV series",
                StringComparison.OrdinalIgnoreCase)
                ? "TV Series Information"
                : "Movie Information";

        var contextualGuidance =
            mediaType.Equals(
                "TV series",
                StringComparison.OrdinalIgnoreCase)
                ? @"For a TV series, classify the intended audience of the series as a whole.
Do not classify individual seasons or episodes.
Consider the show's overall premise, long-term themes, marketing, official rating, and intended demographic."
                : @"For a movie, classify the intended audience of the film as a whole.
Consider the film's marketing, themes, official rating, and intended demographic.";

        return $@"Analyze this {mediaType} and determine its PRIMARY TARGET AUDIENCE, not merely its content rating.

Target audience is different from content appropriateness:
- A family-safe title may still primarily target adults.
- A PG-13 action title may primarily target teenagers.
- An unrated animated special may clearly target children.

{contextualGuidance}

{typeHeading}:
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
6. For TV series, the audience of the overall series rather than isolated episodes

Reply with exactly one lowercase word:
kids
teens
adults";
    }

    /// <summary>
    /// Parses an audience classification from the AI response.
    /// </summary>
    /// <param name="response">The AI response.</param>
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
        try
        {
            var modelsEndpoint = _endpoint.Replace(
                "/chat/completions",
                "/models",
                StringComparison.OrdinalIgnoreCase);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    modelsEndpoint);

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        _apiKey);
            }

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
                    "Failed to fetch OpenAI-compatible models: {StatusCode} - {Error}",
                    response.StatusCode,
                    responseContent);

                return Array.Empty<string>();
            }

            using var responseJson =
                JsonDocument.Parse(responseContent);

            if (!responseJson.RootElement.TryGetProperty(
                    "data",
                    out var dataArray)
                || dataArray.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var models = dataArray
                .EnumerateArray()
                .Where(
                    model =>
                        model.TryGetProperty(
                            "id",
                            out var idElement)
                        && !string.IsNullOrWhiteSpace(
                            idElement.GetString()))
                .Select(
                    model =>
                        model.GetProperty("id").GetString()!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    model => model,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _logger.LogDebug(
                "Found {Count} OpenAI-compatible models",
                models.Length);

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching OpenAI-compatible models: {Message}",
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
