using System;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoParentalTags.Services;

/// <summary>
/// Factory for creating AI service instances.
/// </summary>
public class AiServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiServiceFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public AiServiceFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates an AI service based on the configuration.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>An instance of IAiService.</returns>
    public virtual IAiService CreateService(PluginConfiguration config)
    {
        IAiService service = config.Provider switch
        {
            AiProvider.Gemini => new GeminiService(_loggerFactory.CreateLogger<GeminiService>()),
            AiProvider.OpenAI => new OpenAiService(_loggerFactory.CreateLogger<OpenAiService>()),
            AiProvider.LocalAI => new OpenAiService(_loggerFactory.CreateLogger<OpenAiService>()),
            _ => throw new ArgumentException($"Unknown AI provider: {config.Provider}")
        };

        // Configure the service
        service.SetApiKey(config.ApiKey);

        if (config.Provider == AiProvider.LocalAI)
        {
            service.SetEndpoint(config.ApiEndpoint);
        }
        else if (config.Provider == AiProvider.OpenAI)
        {
            // Use default OpenAI endpoint
            service.SetEndpoint("https://api.openai.com/v1/chat/completions");
        }

        // Set model name for all services
        service.SetModelName(config.ModelName);

        // Set prompt template if provided
        if (!string.IsNullOrEmpty(config.PromptTemplate))
        {
            service.SetPromptTemplate(config.PromptTemplate);
        }

        // Validate prompt template is configured; unlike other settings this is mandatory for classification
        if (string.IsNullOrWhiteSpace(config.PromptTemplate))
        {
            var logger = _loggerFactory.CreateLogger<AiServiceFactory>();
            logger.LogError("Prompt template is not configured in plugin configuration. Set 'PromptTemplate' in plugin settings.");
            throw new InvalidOperationException("Prompt template is not configured. Please configure 'PromptTemplate' in the plugin settings before creating an AI service.");
        }

        return service;
    }
}
