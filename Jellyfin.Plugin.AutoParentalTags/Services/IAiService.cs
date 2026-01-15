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
    /// Sets the prompt template to use for AI requests.
    /// </summary>
    /// <param name="promptTemplate">The prompt template.</param>
    void SetPromptTemplate(string promptTemplate);

    /// <summary>
    /// Analyzes item metadata (movie or series) to determine target audience.
    /// </summary>
    /// <remarks>
    /// Implementations require a non-empty prompt template to be configured (via <see cref="SetPromptTemplate(string)"/>) prior to calling this method.
    /// If no prompt template is set, implementations may throw an <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <param name="itemType">The lowercase item type (e.g., "movie" or "series").</param>
    /// <param name="title">Item title.</param>
    /// <param name="year">Release year.</param>
    /// <param name="overview">Overview/synopsis.</param>
    /// <param name="officialRating">Official rating (if available).</param>
    /// <param name="genres">Genres for the item.</param>
    /// <param name="existingTags">Existing tags on the item.</param>
    /// <param name="studios">Production studios.</param>
    /// <returns>A task representing the asynchronous operation, containing the target audience tag (kids, teens, family, or adults).</returns>
    Task<string?> DetermineTargetAudienceAsync(
        string itemType,
        string title,
        int? year,
        string? overview,
        string? officialRating,
        string[]? genres,
        string[]? existingTags = null,
        string[]? studios = null);

    /// <summary>
    /// Gets a list of available models from the AI service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the list of model names.</returns>
    Task<string[]> GetAvailableModelsAsync();
}
