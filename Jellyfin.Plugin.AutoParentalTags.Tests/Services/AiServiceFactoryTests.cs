using System;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Jellyfin.Plugin.AutoParentalTags.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests.Services;

/// <summary>
/// Tests for the AiServiceFactory class.
/// </summary>
public class AiServiceFactoryTests
{
    /// <summary>
    /// Tests that factory creates GeminiService for Gemini provider.
    /// </summary>
    [Fact]
    public void CreateService_WithGeminiProvider_ShouldReturnGeminiService()
    {
        // Arrange
        var factory = new AiServiceFactory(NullLoggerFactory.Instance);
        var config = new PluginConfiguration
        {
            Provider = AiProvider.Gemini,
            ApiKey = "test-key"
        };

        // Act
        using var service = factory.CreateService(config);

        // Assert
        Assert.IsType<GeminiService>(service);
    }

    /// <summary>
    /// Tests that factory creates OpenAiService for OpenAI provider.
    /// </summary>
    [Fact]
    public void CreateService_WithOpenAIProvider_ShouldReturnOpenAiService()
    {
        // Arrange
        var factory = new AiServiceFactory(NullLoggerFactory.Instance);
        var config = new PluginConfiguration
        {
            Provider = AiProvider.OpenAI,
            ApiKey = "test-key",
            ModelName = "gpt-4"
        };

        // Act
        using var service = factory.CreateService(config);

        // Assert
        Assert.IsType<OpenAiService>(service);
    }

    /// <summary>
    /// Tests that factory creates OpenAiService for LocalAI provider.
    /// </summary>
    [Fact]
    public void CreateService_WithLocalAIProvider_ShouldReturnOpenAiService()
    {
        // Arrange
        var factory = new AiServiceFactory(NullLoggerFactory.Instance);
        var config = new PluginConfiguration
        {
            Provider = AiProvider.LocalAI,
            ApiKey = "test-key",
            ApiEndpoint = "http://localhost:8080",
            ModelName = "local-model"
        };

        // Act
        using var service = factory.CreateService(config);

        // Assert
        Assert.IsType<OpenAiService>(service);
    }

    /// <summary>
    /// Tests that factory throws for invalid provider.
    /// </summary>
    [Fact]
    public void CreateService_WithInvalidProvider_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new AiServiceFactory(NullLoggerFactory.Instance);
        var config = new PluginConfiguration
        {
            Provider = (AiProvider)999, // Invalid provider
            ApiKey = "test-key"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.CreateService(config));
    }

    /// <summary>
    /// Tests that factory can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        // Act
        var factory = new AiServiceFactory(NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(factory);
    }
}
