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
    /// Runs a small self-test using example items and the provided configuration.
    /// Returns classification results and whether each matched the expected category.
    /// </summary>
    [HttpPost("Test")]
    public async Task<ActionResult<TestResponse>> RunTests([FromBody] TestRequest request)
    {
        try
        {
            if (!Enum.TryParse<AiProvider>(request.Provider, true, out var aiProvider))
            {
                return BadRequest($"Invalid provider: {request.Provider}");
            }

            _logger.LogDebug("Running classification tests for provider: {Provider}", aiProvider);

            // Example test items and expected labels
            var examples = new[]
            {
                new { Title = "Aladdin", Year = 1992, Rating = "PG", Genres = new[] { "Animation", "Family" }, Expected = "family" },
                new { Title = "Finding Nemo", Year = 2003, Rating = "G", Genres = new[] { "Animation", "Adventure" }, Expected = "family" },
                new { Title = "John Wick", Year = 2014, Rating = "R", Genres = new[] { "Action", "Thriller" }, Expected = "adults" },
                new { Title = "Spider-Man", Year = 2002, Rating = "PG-13", Genres = new[] { "Action", "Superhero" }, Expected = "teens" }
            };

            var tempConfig = new PluginConfiguration
            {
                Provider = aiProvider,
                ApiKey = request.ApiKey ?? string.Empty,
                ApiEndpoint = request.Endpoint ?? "http://localhost:8080",
                ModelName = request.ModelName ?? string.Empty,
                PromptTemplate = request.PromptTemplate
            };

            using var aiService = _aiServiceFactory.CreateService(tempConfig);

            var results = new System.Collections.Generic.List<TestResult>();

            foreach (var ex in examples)
            {
                try
                {
                    var actual = await aiService.DetermineTargetAudienceAsync(
                        "movie",
                        ex.Title,
                        ex.Year,
                        $"Sample overview for {ex.Title}",
                        ex.Rating,
                        ex.Genres,
                        null,
                        null).ConfigureAwait(false);

                    var success = !string.IsNullOrEmpty(actual) && actual.Equals(ex.Expected, System.StringComparison.OrdinalIgnoreCase);

                    results.Add(new TestResult
                    {
                        Title = ex.Title,
                        Expected = ex.Expected,
                        Actual = actual,
                        Success = success
                    });
                }
                catch (System.Exception exx)
                {
                    results.Add(new TestResult
                    {
                        Title = ex.Title,
                        Expected = ex.Expected,
                        Actual = null,
                        Success = false,
                        Error = exx.Message
                    });
                }
            }

            return Ok(new TestResponse { Results = results.ToArray() });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error running classification tests: {Message}", ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred while running tests." });
        }
    }

    /// <summary>
    /// Validates the supplied plugin configuration. Ensures required fields like PromptTemplate are present.
    /// </summary>
    [HttpPost("ValidateConfig")]
    public ActionResult ValidateConfig([FromBody] Configuration.PluginConfiguration config)
    {
        if (config == null)
        {
            return BadRequest(new { error = "Configuration is required." });
        }

        if (string.IsNullOrWhiteSpace(config.PromptTemplate))
        {
            return BadRequest(new { error = "PromptTemplate must not be empty." });
        }

        return Ok();
    }
    /// <summary>
    /// Gets available models for the specified provider.
    /// </summary>
    /// <param name="request">The request containing provider, API key, and endpoint.</param>
    /// <returns>Array of model names.</returns>
    [HttpPost("Models")]
    public async Task<ActionResult<string[]>> GetModels([FromBody] ModelsRequest request)
    {
        try
        {
            if (!Enum.TryParse<AiProvider>(request.Provider, true, out var aiProvider))
            {
                return BadRequest($"Invalid provider: {request.Provider}");
            }

            _logger.LogDebug("Fetching models for provider: {Provider}", aiProvider);

            // Create temporary config for fetching models
            var tempConfig = new PluginConfiguration
            {
                Provider = aiProvider,
                ApiKey = request.ApiKey ?? string.Empty,
                ApiEndpoint = request.Endpoint ?? "http://localhost:8080"
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
