using System;
using System.Globalization;
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
    private string _modelName = "gemini-pro";

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiService"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{GeminiService}"/> interface.</param>
    public GeminiService(ILogger<GeminiService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Jellyfin.Plugin.AutoParentalTags/1.0");
    }

    /// <inheritdoc />
    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
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
        }
    }

    /// <summary>
    /// Analyzes movie metadata to determine target audience.
    /// </summary>
    /// <param name="title">Movie title.</param>
    /// <param name="year">Release year.</param>
    /// <param name="overview">Movie overview/synopsis.</param>
    /// <param name="officialRating">Official MPAA rating (if available).</param>
    /// <param name="genres">Movie genres.</param>
    /// <returns>A task representing the asynchronous operation, containing the target audience tag.</returns>
    public async Task<string?> DetermineTargetAudienceAsync(
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured");
            return null;
        }

        try
        {
            var prompt = BuildPrompt(title, year, overview, officialRating, genres);

            _logger.LogDebug("Requesting audience classification for '{Title}' ({Year})", title, year);

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

            // Use header for API key instead of URL parameter to prevent logging/exposure
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent";
            _httpClient.DefaultRequestHeaders.Remove("x-goog-api-key");
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);

            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError(
                    "Gemini API error for '{Title}': {StatusCode} - {Error}",
                    title,
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
                        var tag = ParseAudienceTag(responseText);
                        _logger.LogInformation("Classified '{Title}' ({Year}) as '{Tag}'", title, year, tag);
                        return tag;
                    }
                }
            }

            _logger.LogWarning("No valid response from Gemini API for '{Title}'", title);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for '{Title}': {Message}", title, ex.Message);
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
            // Clear sensitive headers before disposal
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("x-goog-api-key");
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
            catch
            {
                // Ignore errors during cleanup
            }

            _httpClient?.Dispose();
        }
    }
}
