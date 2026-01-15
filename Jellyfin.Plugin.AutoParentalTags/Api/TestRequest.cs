namespace Jellyfin.Plugin.AutoParentalTags.Api;

/// <summary>
/// Request model for running classification tests.
/// </summary>
public class TestRequest
{
    /// <summary>
    /// Gets or sets the AI provider (Gemini, OpenAI, LocalAI).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for the provider (optional).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint for the provider (optional).
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the model name to use (optional).
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Gets or sets the prompt template used for classification.
    /// </summary>
    public string PromptTemplate { get; set; } = string.Empty;
}
