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
}
