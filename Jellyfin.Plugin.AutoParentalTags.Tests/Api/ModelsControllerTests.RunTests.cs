using System.Threading.Tasks;
using Jellyfin.Plugin.AutoParentalTags.Api;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Jellyfin.Plugin.AutoParentalTags.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.AutoParentalTags.Tests.Api;

public partial class ModelsControllerTests
{
    [Fact]
    public async Task RunTests_WithValidProvider_ShouldReturnResults()
    {
        // Arrange
        var mockService = new Mock<IAiService>();

        // Return a sequence of results matching examples order (family, family, adults, teens)
        mockService.SetupSequence(x => x.DetermineTargetAudienceAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<string[]?>(), It.IsAny<string[]?>()))
            .ReturnsAsync("family")
            .ReturnsAsync("family")
            .ReturnsAsync("adults")
            .ReturnsAsync("teens");

        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        mockFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Returns(mockService.Object);

        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        var request = new TestRequest
        {
            Provider = "Gemini",
            ApiKey = "test-key",
            PromptTemplate = "Test template"
        };

        // Act
        var result = await controller.RunTests(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TestResponse>(okResult.Value);
        Assert.Equal(4, response.Results.Count);
        Assert.All(response.Results, r => Assert.NotNull(r.Title));
        Assert.True(response.Results[0].Success);
        Assert.True(response.Results[1].Success);
        Assert.True(response.Results[2].Success);
        Assert.True(response.Results[3].Success);
    }

    [Fact]
    public async Task RunTests_WithInvalidProvider_ShouldReturnBadRequest()
    {
        // Arrange
        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        var request = new TestRequest
        {
            Provider = "InvalidProvider",
            ApiKey = "test",
            PromptTemplate = "Test template"
        };

        // Act
        var result = await controller.RunTests(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid provider", badRequestResult.Value?.ToString());
    }

    [Fact]
    public void ValidateConfig_WithEmptyPrompt_ShouldReturnBadRequest()
    {
        // Arrange
        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        var config = new PluginConfiguration
        {
            PromptTemplate = string.Empty
        };

        // Act
        var result = controller.ValidateConfig(config);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("PromptTemplate must not be empty", badRequest.Value?.ToString());
    }

    [Fact]
    public void ValidateConfig_WithValidPrompt_ShouldReturnOk()
    {
        // Arrange
        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        var config = new PluginConfiguration
        {
            PromptTemplate = "Non-empty prompt"
        };

        // Act
        var result = controller.ValidateConfig(config);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RunTests_WhenServiceThrows_ShouldReturnPartialErrors()
    {
        // Arrange
        var mockService = new Mock<IAiService>();
        mockService.Setup(x => x.DetermineTargetAudienceAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<string[]?>(), It.IsAny<string[]?>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException("Network error"));

        var mockFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        mockFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Returns(mockService.Object);

        var controller = new ModelsController(
            mockFactory.Object,
            NullLogger<ModelsController>.Instance);

        var request = new TestRequest
        {
            Provider = "Gemini",
            ApiKey = "test-key",
            PromptTemplate = "Test template"
        };

        // Act
        var result = await controller.RunTests(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TestResponse>(okResult.Value);
        Assert.Equal(4, response.Results.Count);
        Assert.All(response.Results, r => Assert.False(r.Success));
        Assert.All(response.Results, r => Assert.NotNull(r.Error));
    }
}
