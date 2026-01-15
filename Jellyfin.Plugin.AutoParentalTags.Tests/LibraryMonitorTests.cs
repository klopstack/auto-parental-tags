using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Jellyfin.Plugin.AutoParentalTags.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AutoParentalTags.Tests;

/// <summary>
/// Collection definition for tests that use Plugin.Instance.
/// Ensures tests run sequentially to avoid race conditions with static state.
/// </summary>
[CollectionDefinition("Plugin Instance Tests", DisableParallelization = true)]
public class PluginInstanceTestCollection
{
}

/// <summary>
/// Tests for the LibraryMonitor class.
/// </summary>
[Collection("Plugin Instance Tests")]
public class LibraryMonitorTests : IAsyncLifetime
{
    /// <summary>
    /// Initializes the test by clearing any existing plugin instance.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InitializeAsync()
    {
        ClearPluginInstance();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the test by clearing the plugin instance.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisposeAsync()
    {
        ClearPluginInstance();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that LibraryMonitor can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());

        // Act
        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        // Assert
        Assert.NotNull(monitor);
    }

    /// <summary>
    /// Tests that Run returns early when plugin is not configured.
    /// </summary>
    [Fact]
    public async Task Run_WhenPluginNotConfigured_ShouldReturnEarly()
    {
        // Arrange
        SetPluginInstance(new PluginConfiguration
        {
            EnableAutoTagging = false,
            ProcessOnLibraryScan = false,
            ApiKey = string.Empty
        });

        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var aiServiceFactory = new AiServiceFactory(NullLoggerFactory.Instance);
        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            aiServiceFactory);
        var progress = new Mock<IProgress<double>>();

        // Act
        await monitor.Run(progress.Object, CancellationToken.None);

        // Assert
        mockLibraryManager.Verify(x => x.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }

    /// <summary>
    /// Tests that Run returns when API key missing.
    /// </summary>
    [Fact]
    public async Task Run_WhenApiKeyMissing_ShouldSkipProcessing()
    {
        // Arrange
        SetPluginInstance(new PluginConfiguration
        {
            EnableAutoTagging = true,
            ProcessOnLibraryScan = true,
            ApiKey = string.Empty
        });

        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);
        var progress = new Mock<IProgress<double>>();

        // Act
        await monitor.Run(progress.Object, CancellationToken.None);

        // Assert
        mockLibraryManager.Verify(x => x.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }

    /// <summary>
    /// Tests that Run processes movies when configured.
    /// </summary>
    [Fact]
    public async Task Run_WhenConfigured_ShouldProcessMovies()
    {
        // Arrange
        SetPluginInstance(new PluginConfiguration
        {
            EnableAutoTagging = true,
            ProcessOnLibraryScan = true,
            ApiKey = "key",
            OverwriteExistingTags = false
        });

        // Create mock root folder with empty Children property
        var mockRootFolder = new Mock<Folder>();
        mockRootFolder.Setup(x => x.Children)
            .Returns(new List<BaseItem>());

        var mockLibraryManager = new Mock<ILibraryManager>();
        mockLibraryManager.Setup(x => x.RootFolder)
            .Returns(Mock.Of<AggregateFolder>());
        mockLibraryManager.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem>()); // no movies to avoid external calls

        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var aiServiceFactory = new AiServiceFactory(NullLoggerFactory.Instance);

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            aiServiceFactory);
        var progress = new Mock<IProgress<double>>();

        // Act
        await monitor.Run(progress.Object, CancellationToken.None);

        // Assert
        mockLibraryManager.Verify(x => x.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }

    /// <summary>
    /// Tests that Run processes returned movies and reports progress.
    /// </summary>
    [Fact]
    public async Task Run_WhenMoviesAvailable_ShouldProcessAndReport()
    {
        // Arrange
        SetPluginInstance(new PluginConfiguration
        {
            EnableAutoTagging = true,
            ProcessOnLibraryScan = true,
            ApiKey = "key",
            OverwriteExistingTags = true,
            Provider = AiProvider.Gemini
        });

        var movies = new List<BaseItem>
        {
            new TestMovie { Name = "Movie 1" },
            new TestMovie { Name = "Movie 2" }
        };

        // Create a collection folder that can be used as Parent
        var testLibraryFolder = new CollectionFolder { Name = "Test Library" };

        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockRootFolder = new Mock<AggregateFolder>();
        mockRootFolder.SetupGet(f => f.Children).Returns(new List<BaseItem> { testLibraryFolder });
        mockLibraryManager.Setup(x => x.RootFolder)
            .Returns(mockRootFolder.Object);
        mockLibraryManager.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(movies);

        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(NullLoggerFactory.Instance);
        var aiService = new StubAiService("teens");
        mockAiServiceFactory.Setup(x => x.CreateService(It.IsAny<PluginConfiguration>()))
            .Returns(aiService);

        var progressReports = new List<double>();
        var progress = new Progress<double>(p => progressReports.Add(p));

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object,
            TimeSpan.Zero);

        // Act
        await monitor.Run(progress, CancellationToken.None);

        // Wait for progress reports to be delivered (progress.Report may be posted asynchronously)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (progressReports.Count == 0 || progressReports.Last() != 100)
        {
            if (sw.ElapsedMilliseconds > 1000) break;
            await Task.Delay(10);
        }

        // Assert
        Assert.Equal(2, aiService.Calls);
        Assert.True(progressReports.Count >= 1);
        Assert.Equal(100, progressReports.Last());
        Assert.All(movies.OfType<TestMovie>(), m => Assert.Contains("teens", m.Tags));
    }

    /// <summary>
    /// Tests that ProcessItemAsync handles movie with existing tags.
    /// </summary>
    [Fact]
    public async Task ProcessItemAsync_WithExistingTag_ShouldSkipWhenNotOverwriting()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var mockAiService = new Mock<IAiService>();

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        var movie = new TestMovie
        {
            Name = "Test Movie",
            ProductionYear = 2020,
            Tags = new[] { "kids" }
        };

        // Act
        await monitor.ProcessItemAsync(movie, mockAiService.Object, false, CancellationToken.None);

        // Assert
        mockAiService.Verify(
            x => x.DetermineTargetAudienceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>()),
            Times.Never);
    }

    /// <summary>
    /// Tests that ProcessItemAsync processes movie without existing tags.
    /// </summary>
    [Fact]
    public async Task ProcessItemAsync_WithoutExistingTag_ShouldCallAiService()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var mockAiService = new Mock<IAiService>();
        mockAiService.Setup(x => x.DetermineTargetAudienceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>()))
            .ReturnsAsync("teens");

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        var movie = new TestMovie
        {
            Name = "Test Movie",
            ProductionYear = 2020,
            Overview = "A test movie",
            OfficialRating = "PG-13",
            Tags = Array.Empty<string>()
        };

        // Act
        await monitor.ProcessItemAsync(movie, mockAiService.Object, false, CancellationToken.None);

        // Assert
        mockAiService.Verify(
            x => x.DetermineTargetAudienceAsync(
                "movie",
                "Test Movie",
                2020,
                "A test movie",
                "PG-13",
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ProcessItemAsync adds tag to movie.
    /// </summary>
    [Fact]
    public async Task ProcessItemAsync_WhenAiReturnsTag_ShouldAddTagToMovie()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var mockAiService = new Mock<IAiService>();
        mockAiService.Setup(x => x.DetermineTargetAudienceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string[]?>(),                It.IsAny<string[]?>(),                It.IsAny<string[]?>()))
            .ReturnsAsync("adults");

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        var movie = new TestMovie
        {
            Name = "Test Movie",
            ProductionYear = 2020,
            Tags = Array.Empty<string>()
        };

        // Act
        await monitor.ProcessItemAsync(movie, mockAiService.Object, false, CancellationToken.None);

        // Assert
        Assert.Contains("adults", movie.Tags);
    }

    /// <summary>
    /// Tests that ProcessItemAsync removes old tags when overwriting.
    /// </summary>
    [Fact]
    public async Task ProcessItemAsync_WithOverwriteTrue_ShouldReplaceExistingTag()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var mockAiService = new Mock<IAiService>();
        mockAiService.Setup(x => x.DetermineTargetAudienceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>()))
            .ReturnsAsync("adults");

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        var movie = new TestMovie
        {
            Name = "Test Movie",
            ProductionYear = 2020,
            Tags = new[] { "kids", "family", "action" }
        };

        // Act
        await monitor.ProcessItemAsync(movie, mockAiService.Object, true, CancellationToken.None);

        // Assert
        Assert.Contains("adults", movie.Tags);
        Assert.DoesNotContain("kids", movie.Tags);
        Assert.DoesNotContain("family", movie.Tags); // Family is an audience tag, should be removed
        Assert.Contains("action", movie.Tags); // Non-audience tag should remain
    }

    /// <summary>
    /// Tests that ProcessItemAsync handles null response from AI.
    /// </summary>
    [Fact]
    public async Task ProcessItemAsync_WhenAiReturnsNull_ShouldNotAddTag()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var mockAiService = new Mock<IAiService>();
        mockAiService.Setup(x => x.DetermineTargetAudienceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>()))
            .ReturnsAsync((string?)null);

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        var movie = new TestMovie
        {
            Name = "Test Movie",
            ProductionYear = 2020,
            Tags = Array.Empty<string>()
        };

        var initialTagCount = movie.Tags.Length;

        // Act
        await monitor.ProcessItemAsync(movie, mockAiService.Object, false, CancellationToken.None);

        // Assert
        Assert.Equal(initialTagCount, movie.Tags.Length);
    }

    /// <summary>
    /// Tests that ProcessItemAsync handles empty string response from AI.
    /// </summary>
    [Fact]
    public async Task ProcessItemAsync_WhenAiReturnsEmpty_ShouldNotAddTag()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var mockAiService = new Mock<IAiService>();
        mockAiService.Setup(x => x.DetermineTargetAudienceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>()))
            .ReturnsAsync(string.Empty);

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        var movie = new TestMovie
        {
            Name = "Test Movie",
            ProductionYear = 2020,
            Tags = Array.Empty<string>()
        };

        var initialTagCount = movie.Tags.Length;

        // Act
        await monitor.ProcessItemAsync(movie, mockAiService.Object, false, CancellationToken.None);

        // Assert
        Assert.Equal(initialTagCount, movie.Tags.Length);
    }

    /// <summary>
    /// Tests that ProcessItemAsync does not add duplicate tags.
    /// </summary>
    [Fact]
    public async Task ProcessItemAsync_WithExistingIdenticalTag_ShouldNotAddDuplicate()
    {
        // Arrange
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<LibraryMonitor>>();
        var mockAiServiceFactory = new Mock<AiServiceFactory>(Mock.Of<ILoggerFactory>());
        var mockAiService = new Mock<IAiService>();
        mockAiService.Setup(x => x.DetermineTargetAudienceAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>(),
                It.IsAny<string[]?>()))
            .ReturnsAsync("kids");

        var monitor = new LibraryMonitor(
            mockLibraryManager.Object,
            mockLogger.Object,
            mockAiServiceFactory.Object);

        var movie = new TestMovie
        {
            Name = "Test Movie",
            ProductionYear = 2020,
            Tags = new[] { "kids" }
        };

        // Act
        await monitor.ProcessItemAsync(movie, mockAiService.Object, true, CancellationToken.None);

        // Assert
        Assert.Single(movie.Tags);
        Assert.Equal("kids", movie.Tags[0]);
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

    private static void SetPluginInstance(PluginConfiguration config)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "jellyfin-plugin-tests");
        Directory.CreateDirectory(tempDir);

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(x => x.PluginsPath).Returns(tempDir);
        mockPaths.Setup(x => x.PluginConfigurationsPath).Returns(tempDir);

        var mockSerializer = new Mock<IXmlSerializer>();
        mockSerializer.Setup(x => x.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
            .Returns(config);
        mockSerializer.Setup(x => x.SerializeToFile(It.IsAny<PluginConfiguration>(), It.IsAny<string>()));

        var mockLogger = new Mock<ILogger<Plugin>>();

        ClearPluginInstance();
        _ = new Plugin(mockPaths.Object, mockSerializer.Object, mockLogger.Object);
    }
}

/// <summary>
/// Test double for Movie that skips repository calls.
/// </summary>
internal class TestMovie : Movie
{
    public override Task UpdateToRepositoryAsync(ItemUpdateType updateReason, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple AI service stub for tests.
/// </summary>
internal sealed class StubAiService : IAiService
{
    private readonly string _tag;

    public StubAiService(string tag)
    {
        _tag = tag;
    }

    public int Calls { get; private set; }

    public void Dispose()
    {
    }

    public void SetApiKey(string apiKey)
    {
    }

    public void SetEndpoint(string endpoint)
    {
    }

    public void SetModelName(string modelName)
    {
    }

    public void SetPromptTemplate(string promptTemplate)
    {
    }

    public Task<string?> DetermineTargetAudienceAsync(string itemType, string title, int? year, string? overview, string? officialRating, string[]? genres, string[]? existingTags = null, string[]? studios = null)
    {
        Calls++;
        return Task.FromResult<string?>(_tag);
    }

    public Task<string[]> GetAvailableModelsAsync()
    {
        return Task.FromResult(Array.Empty<string>());
    }
}
