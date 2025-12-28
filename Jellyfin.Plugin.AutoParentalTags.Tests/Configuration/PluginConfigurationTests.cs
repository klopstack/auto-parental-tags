using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests.Configuration;

/// <summary>
/// Tests for the PluginConfiguration class.
/// </summary>
public class PluginConfigurationTests
{
    /// <summary>
    /// Tests that PluginConfiguration has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var config = new PluginConfiguration();

        // Assert
        Assert.Equal(AiProvider.Gemini, config.Provider);
        Assert.Equal(string.Empty, config.ApiKey);
        Assert.Equal("http://localhost:8080", config.ApiEndpoint);
        Assert.Equal("gemini-pro", config.ModelName);
        Assert.True(config.EnableAutoTagging);
        Assert.True(config.ProcessOnLibraryScan);
        Assert.False(config.OverwriteExistingTags);
    }

    /// <summary>
    /// Tests that Provider can be set and retrieved.
    /// </summary>
    [Theory]
    [InlineData(AiProvider.Gemini)]
    [InlineData(AiProvider.OpenAI)]
    [InlineData(AiProvider.LocalAI)]
    public void Provider_ShouldBeSettable(AiProvider provider)
    {
        // Arrange
        var config = new PluginConfiguration();

        // Act
        config.Provider = provider;

        // Assert
        Assert.Equal(provider, config.Provider);
    }

    /// <summary>
    /// Tests that ApiKey can be set and retrieved.
    /// </summary>
    [Fact]
    public void ApiKey_ShouldBeSettable()
    {
        // Arrange
        var config = new PluginConfiguration();
        const string testKey = "test-api-key-12345";

        // Act
        config.ApiKey = testKey;

        // Assert
        Assert.Equal(testKey, config.ApiKey);
    }

    /// <summary>
    /// Tests that ApiEndpoint can be set and retrieved.
    /// </summary>
    [Fact]
    public void ApiEndpoint_ShouldBeSettable()
    {
        // Arrange
        var config = new PluginConfiguration();
        const string testEndpoint = "http://my-local-ai:8080";

        // Act
        config.ApiEndpoint = testEndpoint;

        // Assert
        Assert.Equal(testEndpoint, config.ApiEndpoint);
    }

    /// <summary>
    /// Tests that ModelName can be set and retrieved.
    /// </summary>
    [Fact]
    public void ModelName_ShouldBeSettable()
    {
        // Arrange
        var config = new PluginConfiguration();
        const string testModel = "gpt-4";

        // Act
        config.ModelName = testModel;

        // Assert
        Assert.Equal(testModel, config.ModelName);
    }

    /// <summary>
    /// Tests that EnableAutoTagging can be toggled.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnableAutoTagging_ShouldBeSettable(bool value)
    {
        // Arrange
        var config = new PluginConfiguration();

        // Act
        config.EnableAutoTagging = value;

        // Assert
        Assert.Equal(value, config.EnableAutoTagging);
    }

    /// <summary>
    /// Tests that ProcessOnLibraryScan can be toggled.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProcessOnLibraryScan_ShouldBeSettable(bool value)
    {
        // Arrange
        var config = new PluginConfiguration();

        // Act
        config.ProcessOnLibraryScan = value;

        // Assert
        Assert.Equal(value, config.ProcessOnLibraryScan);
    }

    /// <summary>
    /// Tests that OverwriteExistingTags can be toggled.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OverwriteExistingTags_ShouldBeSettable(bool value)
    {
        // Arrange
        var config = new PluginConfiguration();

        // Act
        config.OverwriteExistingTags = value;

        // Assert
        Assert.Equal(value, config.OverwriteExistingTags);
    }
}
