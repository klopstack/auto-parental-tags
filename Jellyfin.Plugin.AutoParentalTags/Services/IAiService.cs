using System;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AutoParentalTags.Services;

/// <summary>
/// Interface for AI services that determine target audience.
/// </summary>
public interface IAiService : IDisposable
{
    /// <summary>
    /// Sets the API key or credentials for the AI service.
    /// </summary>
    /// <param name="apiKey">The API key or credentials.</param>
    void SetApiKey(string apiKey);

    /// <summary>
    /// Sets the API endpoint URL for self-hosted services such as LocalAI.
    /// </summary>
    /// <param name="endpoint">The endpoint URL.</param>
    void SetEndpoint(string endpoint);

    /// <summary>
    /// Sets the model name to use for AI requests.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    void SetModelName(string modelName);

    /// <summary>
    /// Analyzes movie or TV-series metadata to determine the target audience.
    /// </summary>
    /// <param name="mediaType">
    /// Human-readable media type, such as movie or TV series.
    /// </param>
    /// <param name="title">The movie or TV-series title.</param>
    /// <param name="year">The release or premiere year.</param>
    /// <param name="overview">The item overview or synopsis.</param>
    /// <param name="officialRating">
    /// The official content rating, when available.
    /// </param>
    /// <param name="genres">The item's genres.</param>
    /// <returns>
    /// A task containing the target audience tag: kids, teens, or adults.
    /// </returns>
    Task<string?> DetermineTargetAudienceAsync(
        string mediaType,
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres);

    /// <summary>
    /// Gets a list of available models from the AI service.
    /// </summary>
    /// <returns>
    /// A task containing the available model names.
    /// </returns>
    Task<string[]> GetAvailableModelsAsync();
}
