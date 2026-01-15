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
/// Service for interacting with Google Gemini API.
/// </summary>
public class GeminiService : IAiService, IDisposable
{
    private readonly ILogger<GeminiService> _logger;
    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private string _modelName = "gemini-2.5-flash-lite";
    private string? _promptTemplate;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiService"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{GeminiService}"/> interface.</param>
    public GeminiService(ILogger<GeminiService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Sanitizes a string for logging to prevent log forging attacks.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>A sanitized string safe for logging.</returns>
    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogDebug("Gemini API key is not configured.");
        }
        else
        {
            _logger.LogDebug("Gemini API key is configured.");
        }
    }

    /// <inheritdoc />
    public void SetEndpoint(string endpoint)
    {
        // Gemini uses a fixed endpoint, this is not used
    }

    /// <summary>
    /// Sets the model name to use for Gemini API calls.
    /// </summary>
    /// <param name="modelName">The model name (e.g., gemini-pro, gemini-1.5-pro, gemini-1.5-flash).</param>
    public void SetModelName(string modelName)
    {
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            _modelName = modelName;
            _logger.LogDebug("Gemini model name set to: {ModelName}", SanitizeForLog(modelName));
        }
    }

    /// <summary>
    /// Sets the prompt template to use for AI requests.
    /// </summary>
    /// <param name="promptTemplate">The prompt template.</param>
    public void SetPromptTemplate(string promptTemplate)
    {
        if (string.IsNullOrWhiteSpace(promptTemplate))
        {
            // Explicitly clear the prompt template when an empty or whitespace value is provided.
            _promptTemplate = null;
            // Treat this as an error: an empty prompt will prevent classification.
            _logger.LogError("Empty or whitespace prompt template provided; clearing existing prompt template.");
            return;
        }

        _promptTemplate = promptTemplate;
        _logger.LogDebug("Prompt template set (length: {Length})", promptTemplate.Length);
    }

    /// <summary>
    /// Analyzes movie metadata to determine target audience.
    /// </summary>
    /// <param name="title">Movie title.</param>
    /// <param name="year">Release year.</param>
    /// <param name="overview">Movie overview/synopsis.</param>
    /// <param name="officialRating">Official MPAA rating (if available).</param>
    /// <param name="genres">Movie genres.</param>
    /// <param name="existingTags">Existing tags on the item.</param>
    /// <param name="studios">Production studios.</param>
    /// <returns>A task representing the asynchronous operation, containing the target audience tag.</returns>
    public async Task<string?> DetermineTargetAudienceAsync(
        string itemType,
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres,
        string[]? existingTags = null,
        string[]? studios = null)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogError("Gemini API key is not configured");
            throw new InvalidOperationException("API key must be configured before determining target audience");
        }

        if (string.IsNullOrEmpty(_promptTemplate))
        {
            _logger.LogError("Prompt template is not configured");
            throw new InvalidOperationException("Prompt template must be configured before determining target audience");
        }

        try
        {
            var prompt = BuildPrompt(itemType, title, year, overview, officialRating, genres, existingTags, studios, _promptTemplate);

            _logger.LogDebug("Requesting audience classification for '{Title}' ({Year})", SanitizeForLog(title), year);
            _logger.LogDebug("Prompt for '{Title}':\n{Prompt}", SanitizeForLog(title), prompt);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError(
                    "Gemini API error for '{Title}': {StatusCode} - {Error}",
                    SanitizeForLog(title),
                    response.StatusCode,
                    errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseJson = JsonDocument.Parse(responseContent);

            var candidates = responseJson.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                var content_property = firstCandidate.GetProperty("content");
                var parts = content_property.GetProperty("parts");
                if (parts.GetArrayLength() > 0)
                {
                    var responseText = parts[0].GetProperty("text").GetString();

                    if (!string.IsNullOrEmpty(responseText))
                    {
                        _logger.LogDebug("Raw Gemini response for '{Title}': {Response}", SanitizeForLog(title), SanitizeForLog(responseText));
                        var tag = ParseAudienceTag(responseText);
                        _logger.LogInformation("Classified '{Title}' ({Year}) as '{Tag}'", SanitizeForLog(title), year, tag);
                        return tag;
                    }
                }
            }

            _logger.LogWarning("No valid response from Gemini API for '{Title}'", SanitizeForLog(title));
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for '{Title}': {Message}", SanitizeForLog(title), ex.Message);
            return null;
        }
    }

    private static string BuildPrompt(
        string itemType,
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres,
        string[]? existingTags,
        string[]? studios,
        string promptTemplate)
    {
        if (string.IsNullOrEmpty(promptTemplate))
        {
            throw new ArgumentNullException(nameof(promptTemplate), "Prompt template cannot be null or empty");
        }

        var itemLower = (itemType ?? "item").ToLowerInvariant();
        var itemCapitalized = char.ToUpperInvariant(itemLower[0]) + itemLower.Substring(1);

        // Replace placeholders
        var prompt = promptTemplate
            .Replace("{itemType}", itemLower, StringComparison.OrdinalIgnoreCase)
            .Replace("{ItemType}", itemCapitalized, StringComparison.Ordinal)
            .Replace("{title}", title, StringComparison.Ordinal)
            .Replace("{year}", year?.ToString(CultureInfo.InvariantCulture) ?? "Unknown", StringComparison.Ordinal)
            .Replace("{studios}", studios?.Length > 0 ? string.Join(", ", studios) : "Unknown", StringComparison.Ordinal)
            .Replace("{rating}", officialRating ?? "Not Rated", StringComparison.Ordinal)
            .Replace("{genres}", genres?.Length > 0 ? string.Join(", ", genres) : "Unknown", StringComparison.Ordinal)
            .Replace("{tags}", existingTags?.Length > 0 ? string.Join(", ", existingTags) : "None", StringComparison.Ordinal)
            .Replace("{overview}", overview ?? "No overview available", StringComparison.Ordinal);

        return prompt;
    }

    private static string ParseAudienceTag(string response)
    {
        // Clean up the response and extract the tag
        response = response.ToLower(CultureInfo.InvariantCulture).Trim();

        // Check for exact single-word matches first
        if (response == "kids" || response == "children")
        {
            return "kids";
        }

        if (response == "teens" || response == "teenagers")
        {
            return "teens";
        }

        if (response == "family")
        {
            return "family";
        }

        if (response == "adults" || response == "mature")
        {
            return "adults";
        }

        // Fall back to contains checks for responses with extra text
        if (response.Contains("kids", StringComparison.OrdinalIgnoreCase)
            || response.Contains("children", StringComparison.OrdinalIgnoreCase))
        {
            return "kids";
        }

        if (response.Contains("teens", StringComparison.OrdinalIgnoreCase)
            || response.Contains("teenagers", StringComparison.OrdinalIgnoreCase))
        {
            return "teens";
        }

        if (response.Contains("family", StringComparison.OrdinalIgnoreCase))
        {
            return "family";
        }

        if (response.Contains("adults", StringComparison.OrdinalIgnoreCase)
            || response.Contains("mature", StringComparison.OrdinalIgnoreCase))
        {
            return "adults";
        }

        // Default to adults if unclear
        return "adults";
    }

    /// <inheritdoc />
    public async Task<string[]> GetAvailableModelsAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured");
            return Array.Empty<string>();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models");
            request.Headers.Add("x-goog-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError(
                    "Failed to fetch Gemini models: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);
                return Array.Empty<string>();
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var responseJson = JsonDocument.Parse(responseContent);

            var models = new List<string>();
            if (responseJson.RootElement.TryGetProperty("models", out var modelsArray))
            {
                models = modelsArray.EnumerateArray()
                    .Where(model => model.TryGetProperty("name", out var nameElement) && !string.IsNullOrEmpty(nameElement.GetString()))
                    .Select(model =>
                    {
                        var fullName = model.GetProperty("name").GetString()!;
                        // Extract model name from "models/gemini-pro" format
                        return fullName.Contains('/', StringComparison.Ordinal) ? fullName.Split('/')[1] : fullName;
                    })
                    .Where(modelName => modelName.Contains("gemini", StringComparison.OrdinalIgnoreCase) &&
                                        !modelName.Contains("embedding", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _logger.LogDebug("Found {Count} Gemini models", models.Count);
            return models.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Gemini models: {Message}", ex.Message);
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
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }
}
