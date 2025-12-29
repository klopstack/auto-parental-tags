using System;
using System.Globalization;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiService"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{OpenAiService}"/> interface.</param>
    public OpenAiService(ILogger<OpenAiService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <inheritdoc />
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
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
        }
    }

    /// <inheritdoc />
    public async Task<string?> DetermineTargetAudienceAsync(
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres)
    {
        try
        {
            var prompt = BuildPrompt(title, year, overview, officialRating, genres);

            _logger.LogDebug("Requesting audience classification for '{Title}' ({Year})", title, year);

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
            using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
            {
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
                        title,
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
                        var tag = ParseAudienceTag(responseText);
                        _logger.LogInformation("Classified '{Title}' ({Year}) as '{Tag}'", title, year, tag);
                        return tag;
                    }
                }

                _logger.LogWarning("No valid response from AI API for '{Title}'", title);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI API for '{Title}': {Message}", title, ex.Message);
            return null;
        }
    }

    private static string BuildPrompt(
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres)
    {
        var prompt = $@"Analyze this movie and determine its TARGET AUDIENCE (not content rating).
Consider that target audience is different from content appropriateness:
- A PG movie from the 1970s might be targeted at adults despite being appropriate for children
- A PG-13 action movie might be targeted specifically at teenagers
- An unrated Christmas special might be clearly targeted at kids

Movie Information:
Title: {title}
Year: {year?.ToString(CultureInfo.InvariantCulture) ?? "Unknown"}
Official Rating: {officialRating ?? "Not Rated"}
Genres: {(genres?.Length > 0 ? string.Join(", ", genres) : "Unknown")}
Overview: {overview ?? "No overview available"}

Respond with ONLY ONE of these three options based on the PRIMARY target audience:
- kids (targeted at children, typically ages 2-11)
- teens (targeted at teenagers, typically ages 12-17)
- adults (targeted at mature audiences, ages 18+)

Consider:
1. The film's marketing and intended demographic
2. Themes and subject matter complexity
3. Historical context (pre-1990 PG films often targeted adults)
4. Whether it's a franchise aimed at kids/teens/adults
5. The sophistication level of storytelling

Respond with just one word: kids, teens, or adults";

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

        if (response.Contains("adults", StringComparison.OrdinalIgnoreCase)
            || response.Contains("mature", StringComparison.OrdinalIgnoreCase))
        {
            return "adults";
        }

        // Default to adults if unclear
        return "adults";
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
