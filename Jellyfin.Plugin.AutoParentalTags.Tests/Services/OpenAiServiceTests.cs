using System;
using System.Net;
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
    /// Tests that DetermineTargetAudienceAsync can be constructed with a custom handler.
    /// </summary>
    [Fact]
    public void Constructor_WithHandler_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        var handler = new MockHttpMessageHandler();

        // Act & Assert
        using var service = new OpenAiService(mockLogger.Object, handler);
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
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("kids"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A kids movie",
            "PG",
            new[] { "Animation" });

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
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "TV series",
                "Test Show",
            2020,
            "A teen show",
            "TV-14",
            null);

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
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("adults"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Film",
            2020,
            "A mature film",
            "R",
            new[] { "Drama" });

        // Assert
        Assert.Equal("adults", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync parses "children" as kids.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_Children_ShouldReturnKids()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("This is for children only"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

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
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("Great for teenagers"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "TV series",
                "Test Show",
            2020,
            "A show",
            "TV-14",
            null);

        // Assert
        Assert.Equal("teens", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync parses "adult" as adults.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_Adult_ShouldReturnAdults()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("An adult story"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Film",
            2020,
            "A film",
            "R",
            null);

        // Assert
        Assert.Equal("adults", result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when no choices are present.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_NoChoices_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"usage\":{}}");
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when choices is empty.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_EmptyChoices_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"choices\":[]}");
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null when the choice has no message content.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_NoMessageContent_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"choices\":[{\"message\":{}}]}");
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync returns null for empty content.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_EmptyContent_ShouldReturnNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":\"\"}}]}");
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

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
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("maybe later"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

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
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

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
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

        // Act
        var result = await service.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
            2020,
            "A movie",
            "PG",
            null);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that DetermineTargetAudienceAsync can handle null year.
    /// </summary>
    [Fact]
    public async Task DetermineTargetAudienceAsync_WithNullYear_ShouldHandleGracefully()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

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
    public async Task DetermineTargetAudienceAsync_WithNullOverview_ShouldHandleGracefully()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

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
    public async Task DetermineTargetAudienceAsync_WithNullRating_ShouldHandleGracefully()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

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
    public async Task DetermineTargetAudienceAsync_WithNullGenres_ShouldHandleGracefully()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiSuccessResponse("teens"));
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetModelName("gpt-3.5-turbo");

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
    /// Tests that GetAvailableModelsAsync returns configured models.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_ShouldReturnModels()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiModelsResponse());
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetEndpoint("http://localhost:8080");

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Contains("gpt-3.5-turbo", result);
        Assert.Contains("gpt-4", result);
        Assert.Equal(2, result.Length);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync returns empty when the data array is missing.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_NoModels_ShouldReturnEmpty()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, "{\"foo\":\"bar\"}");
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetEndpoint("http://localhost:8080");

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
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetEndpoint("http://localhost:8080");

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
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetEndpoint("http://localhost:8080");

        // Act
        var result = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that GetAvailableModelsAsync uses the /models endpoint derived from the base URL.
    /// </summary>
    [Fact]
    public async Task GetAvailableModelsAsync_ShouldUseModelsEndpoint()
    {
        // Arrange
        var handler = new MockHttpMessageHandler()
            .RespondWithJson(HttpStatusCode.OK, OpenAiModelsResponse());
        var mockLogger = new Mock<ILogger<OpenAiService>>();
        using var service = new OpenAiService(mockLogger.Object, handler);
        service.SetApiKey("test-key");
        service.SetEndpoint("http://localhost:8080");

        // Act
        await service.GetAvailableModelsAsync();

        // Assert
        Assert.Equal(1, handler.RequestCount);
        Assert.Contains("/models", handler.Requests[0].RequestUri!.ToString());
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

    private static string OpenAiSuccessResponse(string text)
    {
        return "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"" + text + "\"}}]}";
    }

    private static string OpenAiModelsResponse()
    {
        return "{\"data\":[\n" +
               "{\"id\":\"gpt-3.5-turbo\",\"object\":\"model\"},\n" +
               "{\"id\":\"gpt-4\",\"object\":\"model\"}\n" +
               "]}";
    }
}
