using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests;

/// <summary>
/// Regression tests for post-scan and manual execution behavior.
/// </summary>
[Collection("Plugin Instance Tests")]
public class LibraryMonitorScanBehaviorTests : IAsyncLifetime
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
    /// A normal Jellyfin library scan must not invoke classification when the
    /// post-scan setting is disabled.
    /// </summary>
    [Fact]
    public async Task Run_WhenProcessOnLibraryScanDisabled_ShouldNotQueryLibrary()
    {
        SetPluginInstance(new PluginConfiguration
        {
            EnableAutoTagging = true,
            ProcessOnLibraryScan = false,
            ApiKey = "key"
        });

        var libraryManager = new Mock<ILibraryManager>();
        var aiServiceFactory = new Mock<AiServiceFactory>(NullLoggerFactory.Instance);
        var monitor = new LibraryMonitor(
            libraryManager.Object,
            NullLogger<LibraryMonitor>.Instance,
            aiServiceFactory.Object,
            TimeSpan.Zero);

        await monitor.Run(
            new Progress<double>(),
            CancellationToken.None);

        libraryManager.Verify(
            x => x.GetItemList(It.IsAny<InternalItemsQuery>()),
            Times.Never);
    }

    /// <summary>
    /// The manual scheduled task must remain usable even when automatic
    /// post-library-scan processing is disabled.
    /// </summary>
    [Fact]
    public async Task RunManualAsync_WhenProcessOnLibraryScanDisabled_ShouldQueryLibrary()
    {
        SetPluginInstance(new PluginConfiguration
        {
            EnableAutoTagging = true,
            ProcessOnLibraryScan = false,
            ApiKey = "key"
        });

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem>());

        var aiServiceFactory = new Mock<AiServiceFactory>(NullLoggerFactory.Instance);
        var monitor = new LibraryMonitor(
            libraryManager.Object,
            NullLogger<LibraryMonitor>.Instance,
            aiServiceFactory.Object,
            TimeSpan.Zero);

        await monitor.RunManualAsync(
            new Progress<double>(),
            CancellationToken.None);

        libraryManager.Verify(
            x => x.GetItemList(It.IsAny<InternalItemsQuery>()),
            Times.Once);
    }

    /// <summary>
    /// Previously classified items should be filtered before entering the AI
    /// processing path so an ordinary library scan does not create an AI client
    /// or rate-limit each skipped title.
    /// </summary>
    [Fact]
    public async Task Run_WhenAllItemsAlreadyTagged_ShouldNotCreateAiService()
    {
        SetPluginInstance(new PluginConfiguration
        {
            EnableAutoTagging = true,
            ProcessOnLibraryScan = true,
            ApiKey = "key",
            SkipPreviouslyTagged = true,
            OverwriteExistingTags = false
        });

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem>
            {
                new TestMovie
                {
                    Name = "Already Classified",
                    Tags = new[] { "kids" }
                }
            });

        var aiServiceFactory = new Mock<AiServiceFactory>(NullLoggerFactory.Instance);
        var monitor = new LibraryMonitor(
            libraryManager.Object,
            NullLogger<LibraryMonitor>.Instance,
            aiServiceFactory.Object,
            TimeSpan.FromSeconds(10));

        await monitor.Run(
            new Progress<double>(),
            CancellationToken.None);

        aiServiceFactory.Verify(
            x => x.CreateService(It.IsAny<PluginConfiguration>()),
            Times.Never);
    }

    private static void ClearPluginInstance()
    {
        var instanceProperty = typeof(Plugin).GetProperty(
            "Instance",
            System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic);

        instanceProperty?.SetValue(null, null);
    }

    private static void SetPluginInstance(PluginConfiguration config)
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "jellyfin-plugin-scan-behavior-tests");

        Directory.CreateDirectory(tempDir);

        var paths = new Mock<IApplicationPaths>();
        paths.Setup(x => x.PluginsPath).Returns(tempDir);
        paths.Setup(x => x.PluginConfigurationsPath).Returns(tempDir);

        var serializer = new Mock<IXmlSerializer>();
        serializer
            .Setup(x => x.DeserializeFromFile(
                typeof(PluginConfiguration),
                It.IsAny<string>()))
            .Returns(config);
        serializer
            .Setup(x => x.SerializeToFile(
                It.IsAny<PluginConfiguration>(),
                It.IsAny<string>()));

        var logger = new Mock<ILogger<Plugin>>();

        ClearPluginInstance();
        _ = new Plugin(
            paths.Object,
            serializer.Object,
            logger.Object);
    }
}
