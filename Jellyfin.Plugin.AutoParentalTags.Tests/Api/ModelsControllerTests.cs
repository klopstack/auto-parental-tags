using System;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoParentalTags.Api;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Jellyfin.Plugin.AutoParentalTags.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests.Api;

/// <summary>
/// Tests for ModelsController.
/// </summary>
public class ModelsControllerTests
{
    /// <summary>
    /// Tests that GetModels returns models for valid provider.
    /// </summary>
    [Fact]
    public async Task GetModels_WithGeminiProvider_ShouldReturnModels()
    {
        // Arrange
        var mockService = new Mock<IAiService>();
        mockService.Setup(x => x.GetAvailableModelsAsync())
            .ReturnsAsync(new[] { "gemini-pro", "gemini-1.5-pro" });

        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        mockFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Returns(mockService.Object);

        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        // Act
        var result = await controller.GetModels("Gemini", "test-key");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var models = Assert.IsType<string[]>(okResult.Value);
        Assert.Equal(2, models.Length);
        Assert.Contains("gemini-pro", models);
        Assert.Contains("gemini-1.5-pro", models);
    }

    /// <summary>
    /// Tests that GetModels returns BadRequest for invalid provider.
    /// </summary>
    [Fact]
    public async Task GetModels_WithInvalidProvider_ShouldReturnBadRequest()
    {
        // Arrange
        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        // Act
        var result = await controller.GetModels("InvalidProvider", "test-key");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid provider", badRequestResult.Value?.ToString());
    }

    /// <summary>
    /// Tests that GetModels handles exceptions and returns 500.
    /// </summary>
    [Fact]
    public async Task GetModels_WhenExceptionThrown_ShouldReturn500()
    {
        // Arrange
        var mockService = new Mock<IAiService>();
        mockService.Setup(x => x.GetAvailableModelsAsync())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        mockFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Returns(mockService.Object);

        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        // Act
        var result = await controller.GetModels("Gemini", "test-key");

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    /// <summary>
    /// Tests that GetModels works with OpenAI provider.
    /// </summary>
    [Fact]
    public async Task GetModels_WithOpenAIProvider_ShouldReturnModels()
    {
        // Arrange
        var mockService = new Mock<IAiService>();
        mockService.Setup(x => x.GetAvailableModelsAsync())
            .ReturnsAsync(new[] { "gpt-3.5-turbo", "gpt-4" });

        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        mockFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Returns(mockService.Object);

        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        // Act
        var result = await controller.GetModels("OpenAI", "test-key");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var models = Assert.IsType<string[]>(okResult.Value);
        Assert.Equal(2, models.Length);
    }

    /// <summary>
    /// Tests that GetModels works with LocalAI provider and endpoint.
    /// </summary>
    [Fact]
    public async Task GetModels_WithLocalAIProvider_ShouldUseEndpoint()
    {
        // Arrange
        var mockService = new Mock<IAiService>();
        mockService.Setup(x => x.GetAvailableModelsAsync())
            .ReturnsAsync(new[] { "llama-2-7b", "mistral-7b" });

        PluginConfiguration? capturedConfig = null;
        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        mockFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Callback<PluginConfiguration>(config => capturedConfig = config)
            .Returns(mockService.Object);

        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        // Act
        var result = await controller.GetModels("LocalAI", null, "http://192.168.1.100:8080");

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(capturedConfig);
        Assert.Equal(AiProvider.LocalAI, capturedConfig!.Provider);
        Assert.Equal("http://192.168.1.100:8080", capturedConfig.ApiEndpoint);
    }

    /// <summary>
    /// Tests that GetModels returns empty array when service returns no models.
    /// </summary>
    [Fact]
    public async Task GetModels_WhenNoModelsAvailable_ShouldReturnEmptyArray()
    {
        // Arrange
        var mockService = new Mock<IAiService>();
        mockService.Setup(x => x.GetAvailableModelsAsync())
            .ReturnsAsync(Array.Empty<string>());

        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        mockFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Returns(mockService.Object);

        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        // Act
        var result = await controller.GetModels("Gemini", "test-key");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var models = Assert.IsType<string[]>(okResult.Value);
        Assert.Empty(models);
    }
}
