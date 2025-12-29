using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AutoParentalTags.Configuration;

/// <summary>
/// AI Provider options.
/// </summary>
public enum AiProvider
{
    /// <summary>
    /// Google Gemini AI.
    /// </summary>
    Gemini,

    /// <summary>
    /// OpenAI (GPT-4, GPT-3.5).
    /// </summary>
    OpenAI,

    /// <summary>
    /// LocalAI or other OpenAI-compatible APIs.
    /// </summary>
    LocalAI
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Provider = AiProvider.Gemini;
        ApiKey = string.Empty;
        ApiEndpoint = "http://localhost:8080";
        ModelName = "gemini-pro";
        EnableAutoTagging = true;
        ProcessOnLibraryScan = true;
        OverwriteExistingTags = false;
    }

    /// <summary>
    /// Gets or sets the AI provider.
    /// </summary>
    public AiProvider Provider { get; set; }

    /// <summary>
    /// Gets or sets the API key for the AI service.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint (for LocalAI or custom endpoints).
    /// </summary>
    public string ApiEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the model name (for OpenAI/LocalAI/Gemini).
    /// </summary>
    public string ModelName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether automatic tagging is enabled.
    /// </summary>
    public bool EnableAutoTagging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to process movies on library scan.
    /// </summary>
    public bool ProcessOnLibraryScan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing audience tags.
    /// </summary>
    public bool OverwriteExistingTags { get; set; }
}
