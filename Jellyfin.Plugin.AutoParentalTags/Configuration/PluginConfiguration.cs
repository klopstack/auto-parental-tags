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
        ModelName = "gemini-2.5-flash-lite";
        EnableAutoTagging = true;
        ProcessOnLibraryScan = true;
        OverwriteExistingTags = false;
        PromptTemplate = @"**Persona:** Act as a film classification expert specializing in identifying target audiences based on content and ratings.

**Context:** You are provided with detailed information about a film, including its title, year, studio, rating, genres, and a brief overview. You will also be given a set of definitions for target audience categories (kids, family, teens, adults) and specific classification rules.

**Task:** Analyze the provided film information and, *strictly adhering to the provided definitions and classification rules*, determine its PRIMARY TARGET AUDIENCE.

**Input Data:**
*   **Title:** {title}
*   **Year:** {year}
*   **Studio(s):** {studios}
*   **Official Rating:** {rating}
*   **Genres:** {genres}
*   **Overview:** {overview}

**Definitions:**
*   kids: Primary design for children (2-11). (Ex: 'Bluey', 'Cocomelon')
*   family: Designed for shared viewing; appeals to kids AND parents. (Ex: 'Finding Nemo', 'Elf', 'Encanto', 'Aladdin')
*   teens: Adolescent themes or stylized PG-13 action. (Ex: 'Spider-Man', 'Twilight')
*   adults: Mature themes or R-rated content. (Ex: 'Blade Runner', 'John Wick', 'Die Hard')

**Classification Examples (Follow this logic strictly):**
*   'Aladdin' -> family
*   'Finding Nemo' -> family
*   'Elf' -> family
*   'The Emperor's New Groove' -> family
*   'The Bourne Identity' -> adults
*   'Doctor Strange' -> teens
*   'Rick and Morty' -> adults
*   'Bill & Ted's Excellent Adventure' -> teens

**Classification Rules:**
1.  MANDATORY: All Pixar, Disney Animation, and Dreamworks films are 'family' by default, regardless of ""emotional depth.""
2.  Use 'adults' for R-rated content or mature thrillers (e.g., James Bond, Bourne).
3.  If a film is a ""blockbuster"" or ""holiday classic,"" default to 'family' or 'teens'.
4. Consider the cover art style. If the art features scantily clad women (like classic Bond films) then it's not family.

**Format/Output Constraints:** Respond with *only one word*: kids, teens, family, or adults. Do not include any additional text or explanation.

**Goal:** To accurately categorize films based on their target audience using a defined set of criteria.";
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

    /// <summary>
    /// Gets or sets the prompt template for AI requests.
    /// </summary>
    public string PromptTemplate { get; set; }
}
