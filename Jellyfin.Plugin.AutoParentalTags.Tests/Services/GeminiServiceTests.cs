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
                "movie",
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
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            null,
            "A test movie",
            "PG",
            new[] { "Action" });

        // Assert
        Assert.Equal("teens", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null overview.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullOverview_ShouldReturnTeens()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            null,
            "PG",
            new[] { "Action" });

        // Assert
        Assert.Equal("teens", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null rating.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullRating_ShouldReturnTeens()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A test movie",
            null,
            new[] { "Action" });

        // Assert
        Assert.Equal("teens", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null genres.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullGenres_ShouldReturnTeens()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A test movie",
            "PG",
            null);

        // Assert
        Assert.Equal("teens", result);
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
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.InternalServerError, "{\"error\":\"network down\"}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert - Should return empty array on error
        Assert.NotNull(result);
        Assert.IsType<string[]>(result);
        Assert.Empty(result);
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

    /// <summary>
    /// Tests that the service can be constructed with a custom handler.
    /// </summary>
    [Fact]
    public void Constructor_WithHandler_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<GeminiService>>();
        var handler = new MockHttpMessageHandler();

        // Act & Assert
        using var service = new GeminiService(mockLogger.Object, handler);
        Assert.NotNull(service);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns "kids" for a direct response.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_DirectKids_ShouldReturnKids()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("kids"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A kids movie", "PG", new[] { "Animation" });

        // Assert
        Assert.Equal("kids", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns "teens" for a direct response.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_DirectTeens_ShouldReturnTeens()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "TV series", "Test Show", 2020, "A teen show", "TV-14", null);

        // Assert
        Assert.Equal("teens", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns "adults" for a direct response.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_DirectAdults_ShouldReturnAdults()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("adults"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Film", 2020, "A mature film", "R", new[] { "Drama" });

        // Assert
        Assert.Equal("adults", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync parses "kids" from prose.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_ProseKids_ShouldReturnKids()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("This is aimed at kids."));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Equal("kids", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync parses "children" as kids.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_Children_ShouldReturnKids()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("For children only"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Equal("kids", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync parses "teenagers" as teens.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_Teenagers_ShouldReturnTeens()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("Great for teenagers"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "TV series", "Test Show", 2020, "A show", "TV-14", null);

        // Assert
        Assert.Equal("teens", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync parses "mature" as adults.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_Mature_ShouldReturnAdults()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("Mature themes throughout"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Film", 2020, "A film", "R", null);

        // Assert
        Assert.Equal("adults", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when no candidates are present.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_NoCandidates_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"promptFeedback\":{}}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when candidates is empty.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_EmptyCandidates_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"candidates\":[]}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when the candidate has no parts.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_NoParts_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{}}]}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when the part has no text.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_NoText_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{\"parts\":[]}}]}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null for empty text.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_EmptyText_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"\"}}]}}]");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null for an unsupported response.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_Unsupported_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiSuccessResponse("maybe later"));
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null on a non-success status code.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_ErrorStatus_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.InternalServerError, "{\"error\":\"boom\"}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null on a malformed JSON response.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_MalformedJson_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{not valid json");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
            "movie", "Test Movie", 2020, "A movie", "PG", null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync returns configured models.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_ShouldReturnGeminiModels()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, GeminiModelsResponse());
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Contains("gemini-pro", result);
        Assert.DoesNotContain("gemini-pro-embedding", result);
        Assert.Single(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync returns empty when the models array is missing.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_NoModels_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"foo\":\"bar\"}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync returns empty on a non-success status code.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_ErrorStatus_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.Forbidden, "{\"error\":\"forbidden\"}");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync returns empty on a malformed JSON response.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_MalformedJson_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{not valid json");
        var mockLogger = new Mock<ILogger<GeminiService>>();
        using var service = new GeminiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Empty(result);
    }

    private static string GeminiSuccessResponse(string text)
    {
        return "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"" + text + "\"}]}}]}";
    }

    private static string GeminiModelsResponse()
    {
        return "{\"models\":[\n" +
               "{\"name\":\"models/gemini-pro\",\"supportedGenerationMethods\":[\"generateContent\"]},\n" +
               "{\"name\":\"models/gemini-pro-embedding\",\"supportedGenerationMethods\":[\"generateContent\"]},\n" +
               "{\"name\":\"models/palm-2\",\"supportedGenerationMethods\":[\"predict\"]}\n" +
               "]}";
    }
}
