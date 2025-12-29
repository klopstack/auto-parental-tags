namespace Jellyfin.Plugin.AutoParentalTags.Api;

/// <summary>
/// Request model for fetching AI models.
/// </summary>
public class ModelsRequest
{
    /// <summary>
    /// Gets or sets the AI provider.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint (for LocalAI).
    /// </summary>
    public string? Endpoint { get; set; }
}
