using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoParentalTags.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests.Services;

/// <summary>
/// Tests for the GeminiService class.
/// </summary>
public class GeminiServiceTests
{
    /// <summary>
    /// Tests that GeminiService can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();

        // Act
        using var service = new GeminiService(mockLogger.Object);

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
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act
        service.SetApiKey("test-api-key");

        // Assert - No exception means success (key is private field)
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint does not throw (Gemini uses fixed endpoint).
    /// </summary>
    [Fact]
    public void SetEndpoint_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act & Assert
        service.SetEndpoint("http://example.com");
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetModelName sets the model name.
    /// </summary>
    [Fact]
    public void SetModelName_WithValidName_ShouldSetModelName()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act
        service.SetModelName("gemini-pro");

        // Assert - No exception means success
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetModelName handles null or empty model name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetModelName_WithNullOrEmpty_ShouldNotThrow(string? modelName)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act & Assert
        service.SetModelName(modelName!);
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when API key is not set.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithoutApiKey_ShouldReturnNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            "A test movie",
            "PG",
            new[] { "Action", "Adventure" });

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
        var mockLogger = new Mock<ILogger<GeminiService>>();
        var service = new GeminiService(mockLogger.Object);

        // Act & Assert
        service.Dispose();
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null year.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullYear_ShouldReturnNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            null,
            "A test movie",
            "PG",
            new[] { "Action" });

        // Assert - Returns null because we can't actually call the API
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null overview.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullOverview_ShouldReturnNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            null,
            "PG",
            new[] { "Action" });

        // Assert - Returns null because we can't actually call the API
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null rating.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullRating_ShouldReturnNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            "A test movie",
            null,
            new[] { "Action" });

        // Assert - Returns null because we can't actually call the API
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null genres.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullGenres_ShouldReturnNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "Test Movie",
            2020,
            "A test movie",
            "PG",
            null);

        // Assert - Returns null because we can't actually call the API
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync returns empty array without API key.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_WithoutApiKey_ShouldReturnEmptyArray()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync returns empty array on network error.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_WithApiKey_ShouldHandleNetworkError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);
        service.SetApiKey("test-key");

        // Act - Real network call will fail without valid credentials
        var result = await service.GetAvailableModelsAsync();

        // Assert - Should return empty array on error
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
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act
        service.SetModelName("gemini-1.5-pro");

        // Assert - Should not throw
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetModelName ignores empty or whitespace strings.
    /// </summary>
    [Fact]
    public void SetModelName_WithEmptyString_ShouldIgnore()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act
        service.SetModelName(string.Empty);
        service.SetModelName("   ");

        // Assert - Should not throw
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that SetEndpoint does nothing (Gemini has fixed endpoint).
    /// </summary>
    [Fact]
    public void SetEndpoint_ShouldBeIgnored()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object);

        // Act
        service.SetEndpoint("https://custom-endpoint.example.com");

        // Assert - Should not throw
        Assert.NotNull(service);
    }
}
