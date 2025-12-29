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
    /// Sets the API endpoint URL (for self-hosted services like LocalAI).
    /// </summary>
    /// <param name="endpoint">The endpoint URL.</param>
    void SetEndpoint(string endpoint);

    /// <summary>
    /// Sets the model name to use for AI requests.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    void SetModelName(string modelName);

    /// <summary>
    /// Analyzes movie metadata to determine target audience.
    /// </summary>
    /// <param name="title">Movie title.</param>
    /// <param name="year">Release year.</param>
    /// <param name="overview">Movie overview/synopsis.</param>
    /// <param name="officialRating">Official MPAA rating (if available).</param>
    /// <param name="genres">Movie genres.</param>
    /// <returns>A task representing the asynchronous operation, containing the target audience tag (kids, teens, or adults).</returns>
    Task<string?> DetermineTargetAudienceAsync(
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres);

    /// <summary>
    /// Gets a list of available models from the AI service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the list of model names.</returns>
    Task<string[]> GetAvailableModelsAsync();
}
