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
/// Service for interacting with OpenAI-compatible APIs (OpenAI, LocalAI, etc.).
/// </summary>
public class OpenAiService : IAiService, IDisposable
{
    private readonly ILogger<OpenAiService> _logger;
    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private string _endpoint = "https://api.openai.com/v1/chat/completions";
    private string _modelName = "gpt-3.5-turbo";
    private string? _promptTemplate;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiService"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{OpenAiService}"/> interface.</param>
    public OpenAiService(ILogger<OpenAiService> logger)
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
            _logger.LogDebug("OpenAI API key is not configured or is empty.");
        }
        else
        {
            _logger.LogDebug("OpenAI API key is configured.");
        }
    }

    /// <inheritdoc />
    public void SetEndpoint(string endpoint)
    {
        if (!string.IsNullOrEmpty(endpoint))
        {
            // Ensure endpoint ends with proper path
            _endpoint = endpoint.TrimEnd('/');
            if (!_endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                _endpoint += "/v1/chat/completions";
            }

            _logger.LogInformation("OpenAI endpoint configured: {Endpoint}", SanitizeForLog(_endpoint));
        }
    }

    /// <summary>
    /// Sets the model name to use.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    public void SetModelName(string modelName)
    {
        if (!string.IsNullOrEmpty(modelName))
        {
            _modelName = modelName;
            _logger.LogDebug("OpenAI model name set to: {ModelName}", SanitizeForLog(modelName));
        }
    }

    /// <summary>
    /// Sets the prompt template to use for AI requests.
    /// </summary>
    /// <param name="promptTemplate">The prompt template.</param>
    public void SetPromptTemplate(string promptTemplate)
    {
        if (!string.IsNullOrWhiteSpace(promptTemplate))
        {
            _promptTemplate = promptTemplate;
            _logger.LogDebug("Prompt template set (length: {Length})", promptTemplate.Length);
        }
    }

    /// <inheritdoc />
    public async Task<string?> DetermineTargetAudienceAsync(
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres,
        string[]? existingTags = null,
        string[]? studios = null)
    {
        if (string.IsNullOrEmpty(_promptTemplate))
        {
            _logger.LogError("Prompt template is not configured");
            throw new InvalidOperationException("Prompt template must be configured before determining target audience");
        }

        try
        {
            var prompt = BuildPrompt(title, year, overview, officialRating, genres, existingTags, studios, _promptTemplate);

            _logger.LogDebug("Requesting audience classification for '{Title}' ({Year})", SanitizeForLog(title), year);
            _logger.LogDebug("Prompt for '{Title}':\n{Prompt}", SanitizeForLog(title), prompt);

            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a movie analyst that determines the target audience for films."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.3,
                max_tokens = 10
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Add authorization header if API key is provided
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }

            var response = await _httpClient.PostAsync(_endpoint, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError(
                    "AI API error for '{Title}': {StatusCode} - {Error}",
                    SanitizeForLog(title),
                    response.StatusCode,
                    errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseJson = JsonDocument.Parse(responseContent);

            var choices = responseJson.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var responseText = message.GetProperty("content").GetString();

                if (!string.IsNullOrEmpty(responseText))
                {
                    _logger.LogDebug("Raw API response for '{Title}': {Response}", SanitizeForLog(title), SanitizeForLog(responseText));
                    var tag = ParseAudienceTag(responseText);
                    _logger.LogInformation("Classified '{Title}' ({Year}) as '{Tag}'", SanitizeForLog(title), year, tag);
                    return tag;
                }
            }

            _logger.LogWarning("No valid response from AI API for '{Title}'", SanitizeForLog(title));
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI API for '{Title}': {Message}", SanitizeForLog(title), ex.Message);
            return null;
        }
    }

    private static string BuildPrompt(
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

        // Replace placeholders
        var prompt = promptTemplate
            .Replace("{itemType}", "movie", StringComparison.OrdinalIgnoreCase)
            .Replace("{ItemType}", "Movie", StringComparison.Ordinal)
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
        try
        {
            // Build the models endpoint from the chat endpoint
            var modelsEndpoint = _endpoint.Replace("/chat/completions", "/models", StringComparison.Ordinal);

            using var request = new HttpRequestMessage(HttpMethod.Get, modelsEndpoint);

            // Add authorization header if API key is provided
            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            }

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError(
                    "Failed to fetch OpenAI models: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);
                return Array.Empty<string>();
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var responseJson = JsonDocument.Parse(responseContent);

            var models = new List<string>();
            if (responseJson.RootElement.TryGetProperty("data", out var dataArray))
            {
                models = dataArray.EnumerateArray()
                    .Where(model => model.TryGetProperty("id", out var idElement) && !string.IsNullOrEmpty(idElement.GetString()))
                    .Select(model => model.GetProperty("id").GetString()!)
                    .ToList();
            }

            _logger.LogDebug("Found {Count} OpenAI-compatible models", models.Count);
            return models.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OpenAI models: {Message}", ex.Message);
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
