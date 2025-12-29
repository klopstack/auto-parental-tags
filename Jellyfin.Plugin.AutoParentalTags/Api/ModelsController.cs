using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Jellyfin.Plugin.AutoParentalTags.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoParentalTags.Api;

/// <summary>
/// API controller for fetching available AI models.
/// </summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("AutoParentalTags")]
public class ModelsController : ControllerBase
{
    private readonly AiServiceFactory _aiServiceFactory;
    private readonly ILogger<ModelsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsController"/> class.
    /// </summary>
    /// <param name="aiServiceFactory">Instance of the <see cref="AiServiceFactory"/> class.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ModelsController}"/> interface.</param>
    public ModelsController(
        AiServiceFactory aiServiceFactory,
        ILogger<ModelsController> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets available models for the specified provider.
    /// </summary>
    /// <param name="provider">The AI provider.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="endpoint">The API endpoint (for LocalAI/OpenAI).</param>
    /// <returns>Array of model names.</returns>
    [HttpGet("Models")]
    public async Task<ActionResult<string[]>> GetModels(
        [FromQuery] string provider,
        [FromQuery] string? apiKey = null,
        [FromQuery] string? endpoint = null)
    {
        try
        {
            if (!Enum.TryParse<AiProvider>(provider, true, out var aiProvider))
            {
                return BadRequest($"Invalid provider: {provider}");
            }

            _logger.LogDebug("Fetching models for provider: {Provider}", aiProvider);

            // Create temporary config for fetching models
            var tempConfig = new PluginConfiguration
            {
                Provider = aiProvider,
                ApiKey = apiKey ?? string.Empty,
                ApiEndpoint = endpoint ?? "http://localhost:8080"
            };

            using var aiService = _aiServiceFactory.CreateService(tempConfig);
            var models = await aiService.GetAvailableModelsAsync().ConfigureAwait(false);

            _logger.LogInformation("Retrieved {Count} models for {Provider}", models.Length, aiProvider);

            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models: {Message}", ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred while fetching models." });
        }
    }
}
