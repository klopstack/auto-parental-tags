using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests;

/// <summary>
/// Tests for the Plugin class.
/// </summary>
/// <remarks>
/// <see cref="Plugin.Instance"/> is a static field shared with other test
/// collections that construct plugins in parallel. This class clears it before
/// and after each test so the static-instance assertions are not affected by
/// concurrent construction from other collections.
/// </remarks>
public class PluginTests : IAsyncLifetime
{
    /// <inheritdoc />
    public Task InitializeAsync()
    {
        ClearPluginInstance();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        ClearPluginInstance();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that Plugin can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        var mockAppPaths = new Mock<IApplicationPaths>();
        mockAppPaths.Setup(x => x.PluginsPath).Returns("/tmp/plugins");
        mockAppPaths.Setup(x => x.PluginConfigurationsPath).Returns("/tmp/plugins");
        var mockXmlSerializer = new Mock<IXmlSerializer>();
        var mockLogger = new Mock<ILogger<Plugin>>();

        // Act
        var plugin = new Plugin(mockAppPaths.Object, mockXmlSerializer.Object, mockLogger.Object);

        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("Auto Parental Tags", plugin.Name);
        Assert.Equal(Guid.Parse("eb5d7894-8eef-4b36-aa6f-5d124e828ce1"), plugin.Id);
    }

    /// <summary>
    /// Tests that Plugin sets static Instance property.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetStaticInstance()
    {
        // Arrange
        var mockAppPaths = new Mock<IApplicationPaths>();
        mockAppPaths.Setup(x => x.PluginsPath).Returns("/tmp/plugins");
        mockAppPaths.Setup(x => x.PluginConfigurationsPath).Returns("/tmp/plugins");
        var mockXmlSerializer = new Mock<IXmlSerializer>();
        var mockLogger = new Mock<ILogger<Plugin>>();

        // Act
        var plugin = new Plugin(mockAppPaths.Object, mockXmlSerializer.Object, mockLogger.Object);

        // Assert
        Assert.NotNull(Plugin.Instance);
        Assert.Same(plugin, Plugin.Instance);
    }

    /// <summary>
    /// Tests that GetPages returns correct configuration page.
    /// </summary>
    [Fact]
    public void GetPages_ShouldReturnConfigurationPage()
    {
        // Arrange
        var mockAppPaths = new Mock<IApplicationPaths>();
        mockAppPaths.Setup(x => x.PluginsPath).Returns("/tmp/plugins");
        mockAppPaths.Setup(x => x.PluginConfigurationsPath).Returns("/tmp/plugins");
        var mockXmlSerializer = new Mock<IXmlSerializer>();
        var mockLogger = new Mock<ILogger<Plugin>>();
        var plugin = new Plugin(mockAppPaths.Object, mockXmlSerializer.Object, mockLogger.Object);

        // Act
        var pages = plugin.GetPages().ToList();

        // Assert
        Assert.Single(pages);
        Assert.Equal("Auto Parental Tags", pages[0].Name);
        Assert.Contains("Configuration.configPage.html", pages[0].EmbeddedResourcePath);
    }

    /// <summary>
    /// Tests that Plugin name is correct.
    /// </summary>
    [Fact]
    public void Name_ShouldBeAutoParentalTags()
    {
        // Arrange
        var mockAppPaths = new Mock<IApplicationPaths>();
        mockAppPaths.Setup(x => x.PluginsPath).Returns("/tmp/plugins");
        mockAppPaths.Setup(x => x.PluginConfigurationsPath).Returns("/tmp/plugins");
        var mockXmlSerializer = new Mock<IXmlSerializer>();
        var mockLogger = new Mock<ILogger<Plugin>>();
        var plugin = new Plugin(mockAppPaths.Object, mockXmlSerializer.Object, mockLogger.Object);

        // Act & Assert
        Assert.Equal("Auto Parental Tags", plugin.Name);
    }

    /// <summary>
    /// Tests that Plugin ID is consistent.
    /// </summary>
    [Fact]
    public void Id_ShouldBeConsistent()
    {
        // Arrange
        var mockAppPaths = new Mock<IApplicationPaths>();
        mockAppPaths.Setup(x => x.PluginsPath).Returns("/tmp/plugins");
        mockAppPaths.Setup(x => x.PluginConfigurationsPath).Returns("/tmp/plugins");
        var mockXmlSerializer = new Mock<IXmlSerializer>();
        var mockLogger = new Mock<ILogger<Plugin>>();
        var plugin = new Plugin(mockAppPaths.Object, mockXmlSerializer.Object, mockLogger.Object);

        // Act & Assert
        Assert.Equal(Guid.Parse("eb5d7894-8eef-4b36-aa6f-5d124e828ce1"), plugin.Id);
    }

    private static void ClearPluginInstance()
    {
        var instanceProperty = typeof(Plugin).GetProperty(
            "Instance",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        instanceProperty?.SetValue(null, null);
    }
}
