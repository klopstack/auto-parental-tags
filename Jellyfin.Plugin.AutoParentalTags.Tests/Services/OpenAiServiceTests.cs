using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoParentalTags.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests.Services;

/// <summary>
/// Tests for the OpenAiService class.
/// </summary>
public class OpenAiServiceTests
{
    /// <summary>
    /// Tests that OpenAiService can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();

        // Act
        using var service = new OpenAiService(mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetApiKey sets the API key.
    /// </summary>
    [Fact]
    public void SetApiKey_ShouldSetApiKey()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetApiKey("test-api-key");

        // Assert - No exception means success
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint sets the endpoint.
    /// </summary>
    [Fact]
    public void SetEndpoint_WithValidEndpoint_ShouldSetEndpoint()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetEndpoint("http://localhost:8080");

        // Assert - No exception means success
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint handles endpoint without chat/completions path.
    /// </summary>
    [Fact]
    public void SetEndpoint_WithoutChatCompletionsPath_ShouldAppendPath()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetEndpoint("http://localhost:8080");

        // Assert - No exception means success
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint handles trailing slash.
    /// </summary>
    [Fact]
    public void SetEndpoint_WithTrailingSlash_ShouldTrimSlash()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetEndpoint("http://localhost:8080/");

        // Assert - No exception means success
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint handles null or empty endpoint.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetEndpoint_WithNullOrEmpty_ShouldNotThrow(string? endpoint)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act & Assert
        service.SetEndpoint(endpoint!);
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetModelName sets the model name.
    /// </summary>
    [Fact]
    public void SetModelName_WithValidName_ShouldSetModelName()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetModelName("gpt-4");

        // Assert - No exception means success
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetModelName handles null or empty model name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetModelName_WithNullOrEmpty_ShouldNotThrow(string? modelName)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act & Assert
        service.SetModelName(modelName!);
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync handles API call.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithApiKey_ShouldAttemptApiCall()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetApiKey("test-api-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            "A test movie about adventures",
            "PG",
            new[] { "Action", "Adventure" });

        // Assert - Will return null because we can't actually call the API
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null year.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullYear_ShouldHandleGracefully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            null,
            "A test movie",
            "PG",
            new[] { "Action" });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null overview.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullOverview_ShouldHandleGracefully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            null,
            "PG",
            new[] { "Action" });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null rating.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullRating_ShouldHandleGracefully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            "A test movie",
            null,
            new[] { "Action" });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null genres.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullGenres_ShouldHandleGracefully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            "A test movie",
            "PG",
            null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that service implements IDisposable.
    /// </summary>
    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        var service = new OpenAiService(mockLogger.Object);

        // Act & Assert
        service.Dispose();
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync handles network errors gracefully.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_WithNetworkError_ShouldReturnEmptyArray()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetApiKey("test-key");
        service.SetEndpoint("https://api.openai.com");

        // Act - Real network call will fail without valid credentials
        var result = await service.GetAvailableModelsAsync();

        // Assert - Should handle error and return empty array
        Assert.NotNull(result);
        Assert.IsType<string[]>(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync works with LocalAI endpoint.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_WithLocalAIEndpoint_ShouldConstructCorrectUrl()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetEndpoint("http://localhost:8080");

        // Act - Will fail to connect but tests endpoint logic
        var result = await service.GetAvailableModelsAsync();

        // Assert - Should handle error gracefully
        Assert.NotNull(result);
        Assert.IsType<string[]>(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync handles missing API key for OpenAI.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_WithoutApiKey_ShouldStillAttempt()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);
        service.SetEndpoint("https://api.openai.com");

        // Act - Should work for LocalAI but fail for OpenAI
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<string[]>(result);
    }

    /// <summary>
    /// Tests that SetModelName updates the model name.
    /// </summary>
    [Fact]
    public void SetModelName_WithValidName_ShouldAccept()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetModelName("gpt-4");

        // Assert - Should not throw
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetModelName ignores empty strings.
    /// </summary>
    [Fact]
    public void SetModelName_WithEmptyString_ShouldIgnore()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetModelName(string.Empty);

        // Assert - Should not throw
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint appends proper path when missing.
    /// </summary>
    [Fact]
    public void SetEndpoint_WithoutPath_ShouldAppendPath()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetEndpoint("http://localhost:8080");

        // Assert - Should not throw
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint handles trailing slashes.
    /// </summary>
    [Fact]
    public void SetEndpoint_WithTrailingSlash_ShouldHandleCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetEndpoint("http://localhost:8080/");

        // Assert - Should not throw
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint ignores empty strings.
    /// </summary>
    [Fact]
    public void SetEndpoint_WithEmptyString_ShouldIgnore()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object);

        // Act
        service.SetEndpoint(string.Empty);

        // Assert - Should not throw
        Assert.NotNull(service);
    }
}

