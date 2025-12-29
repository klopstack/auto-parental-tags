using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Jellyfin.Plugin.AutoParentalTags.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests;

/// <summary>
/// Tests for AutoParentalTagsScheduledTask.
/// </summary>
public class AutoParentalTagsScheduledTaskTests
{
    /// <summary>
    /// Tests that the task has correct name and metadata.
    /// </summary>
    [Fact]
    public void TaskProperties_ShouldHaveCorrectValues()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockServiceFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        var mockLibraryMonitor = new LibraryMonitor(
            mockLibraryManager.Object,
            NullLogger<LibraryMonitor>.Instance,
            mockServiceFactory.Object);

        // Act
        var task = new AutoParentalTagsScheduledTask(
            mockLibraryMonitor,
            NullLogger<AutoParentalTagsScheduledTask>.Instance);

        // Assert
        Assert.Equal("Auto Parental Tags", task.Name);
        Assert.Equal("AutoParentalTags", task.Key);
        Assert.Equal("Library", task.Category);
        Assert.Contains("target audience", task.Description, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that the task has no default triggers.
    /// </summary>
    [Fact]
    public void GetDefaultTriggers_ShouldReturnEmptyArray()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockServiceFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
        var mockLibraryMonitor = new LibraryMonitor(
            mockLibraryManager.Object,
            NullLogger<LibraryMonitor>.Instance,
            mockServiceFactory.Object);
        var task = new AutoParentalTagsScheduledTask(
            mockLibraryMonitor,
            NullLogger<AutoParentalTagsScheduledTask>.Instance);

        // Act
        var triggers = task.GetDefaultTriggers();

        // Assert
        Assert.Empty(triggers);
    }

    /// <summary>
    /// Tests that ExecuteAsync can be invoked without throwing on valid setup.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithDisabledAutoTagging_ShouldCompleteQuickly()
    {
        // Arrange - Setup plugin with disabled auto-tagging
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "jellyfin-test-" + Guid.NewGuid());
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(x => x.PluginsPath).Returns(tempDir);
            mockPaths.Setup(x => x.PluginConfigurationsPath).Returns(tempDir);

            var config = new PluginConfiguration { EnableAutoTagging = false };
            var mockSerializer = new Mock<IXmlSerializer>();
            mockSerializer.Setup(x => x.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
                .Returns(config);
            var mockLogger = new Mock<ILogger<Plugin>>();

            // Clear and create new plugin instance
            var pluginInstance = Plugin.Instance;
            if (pluginInstance != null)
            {
                var instanceField = typeof(Plugin).GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                instanceField?.SetValue(null, null);
            }

            _ = new Plugin(mockPaths.Object, mockSerializer.Object, mockLogger.Object);

            var mockLibraryManager = new Mock<ILibraryManager>();
            mockLibraryManager.Setup(x => x.GetItemList(It.IsAny<MediaBrowser.Controller.Entities.InternalItemsQuery>()))
                .Returns(new System.Collections.Generic.List<MediaBrowser.Controller.Entities.BaseItem>());

            var mockServiceFactory = new Mock<AiServiceFactory>(MockBehavior.Loose, null!);
            var libraryMonitor = new LibraryMonitor(
                mockLibraryManager.Object,
                NullLogger<LibraryMonitor>.Instance,
                mockServiceFactory.Object,
                TimeSpan.Zero);

            var task = new AutoParentalTagsScheduledTask(
                libraryMonitor,
                NullLogger<AutoParentalTagsScheduledTask>.Instance);

            var progress = new Progress<double>();
            var cts = new CancellationToken();

            // Act
            await task.ExecuteAsync(progress, cts);

            // Assert - Should complete without throwing
            Assert.True(true);
        }
        finally
        {
            // Cleanup
            try
            {
                System.IO.Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
