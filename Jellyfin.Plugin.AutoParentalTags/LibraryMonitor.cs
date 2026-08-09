using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using Jellyfin.Plugin.AutoParentalTags.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoParentalTags;

/// <summary>
/// Monitors library changes and processes movies and TV series.
/// </summary>
public class LibraryMonitor : ILibraryPostScanTask
{
    private static readonly HashSet<string> AudienceTags =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "kids",
            "teens",
            "adults"
        };

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryMonitor> _logger;
    private readonly AiServiceFactory _aiServiceFactory;
    private readonly TimeSpan _processingDelay;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryMonitor"/> class.
    /// </summary>
    /// <param name="libraryManager">
    /// Instance of the <see cref="ILibraryManager"/> interface.
    /// </param>
    /// <param name="logger">
    /// Instance of the <see cref="ILogger{LibraryMonitor}"/> interface.
    /// </param>
    /// <param name="aiServiceFactory">
    /// Instance of the <see cref="AiServiceFactory"/> class.
    /// </param>
    /// <param name="processingDelay">
    /// Optional delay between AI requests.
    /// </param>
    public LibraryMonitor(
        ILibraryManager libraryManager,
        ILogger<LibraryMonitor> logger,
        AiServiceFactory aiServiceFactory,
        TimeSpan? processingDelay = null)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _aiServiceFactory = aiServiceFactory;
        _processingDelay = processingDelay ?? TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Sanitizes a string for logging to prevent log forging attacks.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>A sanitized string safe for logging.</returns>
    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the audience classification tags already assigned to an item.
    /// </summary>
    /// <param name="item">The Jellyfin library item.</param>
    /// <returns>A list of existing audience tags.</returns>
    private static List<string> GetExistingAudienceTags(BaseItem item)
    {
        return item.Tags?
            .Where(tag => AudienceTags.Contains(tag))
            .ToList()
            ?? new List<string>();
    }

    /// <summary>
    /// Gets a human-readable media-type label.
    /// </summary>
    /// <param name="item">The Jellyfin library item.</param>
    /// <returns>The media-type label.</returns>
    private static string GetMediaTypeLabel(BaseItem item)
    {
        return item is Series ? "TV series" : "movie";
    }

    /// <summary>
    /// Gets the item types to include for the configured scan mode.
    /// </summary>
    /// <param name="scanMode">The configured media scan mode.</param>
    /// <returns>An array of Jellyfin item types.</returns>
    private static BaseItemKind[] GetIncludedItemTypes(MediaScanMode scanMode)
    {
        return scanMode switch
        {
            MediaScanMode.TvSeries =>
                new[]
                {
                    BaseItemKind.Series
                },

            MediaScanMode.Both =>
                new[]
                {
                    BaseItemKind.Movie,
                    BaseItemKind.Series
                },

            _ =>
                new[]
                {
                    BaseItemKind.Movie
                }
        };
    }

    /// <inheritdoc />
    public Task Run(
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        return RunCoreAsync(
            progress,
            cancellationToken,
            requireLibraryScanSetting: true,
            triggerName: "library scan");
    }

    /// <summary>
    /// Runs audience classification from the manual Jellyfin scheduled task.
    /// Manual runs are independent of the ProcessOnLibraryScan setting.
    /// </summary>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RunManualAsync(
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        return RunCoreAsync(
            progress,
            cancellationToken,
            requireLibraryScanSetting: false,
            triggerName: "manual task");
    }

    private async Task RunCoreAsync(
        IProgress<double> progress,
        CancellationToken cancellationToken,
        bool requireLibraryScanSetting,
        string triggerName)
    {
        var lockTaken = false;

        try
        {
            lockTaken = await _runLock
                .WaitAsync(0, cancellationToken)
                .ConfigureAwait(false);

            if (!lockTaken)
            {
                _logger.LogInformation(
                    "Auto Parental Tags is already running; skipping overlapping {Trigger} invocation",
                    triggerName);

                progress?.Report(100);
                return;
            }

            PluginConfiguration? config;

            try
            {
                config = Plugin.Instance?.Configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load plugin configuration");

                progress?.Report(100);
                return;
            }

            if (config == null || !config.EnableAutoTagging)
            {
                _logger.LogDebug(
                    "Auto-tagging is disabled");

                progress?.Report(100);
                return;
            }

            if (requireLibraryScanSetting && !config.ProcessOnLibraryScan)
            {
                _logger.LogDebug(
                    "Auto-tagging is not configured to run after a library scan");

                progress?.Report(100);
                return;
            }

            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                _logger.LogWarning(
                    "AI API key is not configured");

                progress?.Report(100);
                return;
            }

            var includedItemTypes =
                GetIncludedItemTypes(config.ScanMode);

            // ILibraryPostScanTask runs after a normal Jellyfin library scan. Keep
            // this query lightweight by selecting only supported top-level media
            // types, then remove duplicate Jellyfin item IDs before processing.
            var items = _libraryManager
                .GetItemList(
                    new InternalItemsQuery
                    {
                        IncludeItemTypes = includedItemTypes,
                        IsVirtualItem = false,
                        Recursive = true
                    })
                .Where(item => item is Movie || item is Series)
                .GroupBy(item => item.Id)
                .Select(group => group.First())
                .ToList();

            if (items.Count == 0)
            {
                progress?.Report(100);

                _logger.LogInformation(
                    "No matching movies or TV series were found for {Trigger}",
                    triggerName);

                return;
            }

            // When existing tags are not meant to be overwritten, eliminate
            // already classified items before creating the AI service or entering
            // the rate-limited loop. Previously these items were logged and then
            // delayed for one second each, turning ordinary Jellyfin scans into
            // long-running full-library jobs.
            var shouldSkipExisting =
                config.SkipPreviouslyTagged
                || !config.OverwriteExistingTags;

            var candidates = shouldSkipExisting
                ? items
                    .Where(item => GetExistingAudienceTags(item).Count == 0)
                    .ToList()
                : items;

            var skippedCount = items.Count - candidates.Count;

            _logger.LogInformation(
                "Auto Parental Tags {Trigger}: examined {Examined} items, {Candidates} require classification, {Skipped} already classified items skipped",
                triggerName,
                items.Count,
                candidates.Count,
                skippedCount);

            if (candidates.Count == 0)
            {
                progress?.Report(100);
                return;
            }

            using var aiService =
                _aiServiceFactory.CreateService(config);

            var completedCount = 0;
            var taggedCount = 0;
            var failedCount = 0;
            var totalCandidates = candidates.Count;

            foreach (var item in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var wasTagged = await ProcessItemAsync(
                            item,
                            aiService,
                            config.OverwriteExistingTags,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (wasTagged)
                    {
                        taggedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;

                    _logger.LogError(
                        ex,
                        "Error processing {MediaType} '{Title}': {Message}",
                        GetMediaTypeLabel(item),
                        SanitizeForLog(item.Name),
                        ex.Message);
                }

                completedCount++;

                progress?.Report(
                    (double)completedCount / totalCandidates * 100);

                // The delay exists only to rate-limit actual AI requests. Do not
                // delay for items that were filtered out as already classified.
                if (completedCount < totalCandidates
                    && _processingDelay > TimeSpan.Zero)
                {
                    await Task.Delay(
                            _processingDelay,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            _logger.LogInformation(
                "Completed audience classification for {Trigger}. Examined: {Examined}, AI candidates: {Candidates}, tagged: {Tagged}, skipped: {Skipped}, failed: {Failed}",
                triggerName,
                items.Count,
                totalCandidates,
                taggedCount,
                skippedCount,
                failedCount);

            progress?.Report(100);
        }
        finally
        {
            if (lockTaken)
            {
                _runLock.Release();
            }
        }
    }

    /// <summary>
    /// Processes a movie or TV series and applies an audience tag.
    /// </summary>
    /// <param name="item">The movie or TV series to process.</param>
    /// <param name="aiService">The AI service to use.</param>
    /// <param name="overwriteExisting">
    /// Whether existing audience tags should be replaced.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// True when a new classification was applied; otherwise false.
    /// </returns>
    public async Task<bool> ProcessItemAsync(
        BaseItem item,
        IAiService aiService,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        if (item is not Movie && item is not Series)
        {
            _logger.LogDebug(
                "Skipping unsupported item type {ItemType} for '{Title}'",
                item.GetType().Name,
                SanitizeForLog(item.Name));

            return false;
        }

        var mediaType = GetMediaTypeLabel(item);
        var existingAudienceTags =
            GetExistingAudienceTags(item);

        if (existingAudienceTags.Count > 0
            && !overwriteExisting)
        {
            _logger.LogDebug(
                "{MediaType} '{Title}' already has audience tag(s): {Tags}",
                mediaType,
                SanitizeForLog(item.Name),
                string.Join(", ", existingAudienceTags));

            return false;
        }

        var title = item.Name;
        var year = item.ProductionYear;
        var overview = item.Overview;
        var rating = item.OfficialRating;
        var genres = item.Genres?.ToArray();

        var audienceTag =
            await aiService.DetermineTargetAudienceAsync(
                    mediaType,
                    title,
                    year,
                    overview,
                    rating,
                    genres)
                .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(audienceTag))
        {
            _logger.LogWarning(
                "Could not determine audience for {MediaType} '{Title}'",
                mediaType,
                SanitizeForLog(title));

            return false;
        }

        audienceTag = audienceTag.Trim().ToLowerInvariant();

        if (!AudienceTags.Contains(audienceTag))
        {
            _logger.LogWarning(
                "AI returned unsupported audience tag '{Tag}' for {MediaType} '{Title}'",
                SanitizeForLog(audienceTag),
                mediaType,
                SanitizeForLog(title));

            return false;
        }

        var currentTags =
            item.Tags?.ToList()
            ?? new List<string>();

        if (overwriteExisting
            && existingAudienceTags.Count > 0)
        {
            currentTags.RemoveAll(
                tag => AudienceTags.Contains(tag));
        }

        if (!currentTags.Contains(
                audienceTag,
                StringComparer.OrdinalIgnoreCase))
        {
            currentTags.Add(audienceTag);
        }

        item.Tags = currentTags.ToArray();

        await item.UpdateToRepositoryAsync(
                ItemUpdateType.MetadataEdit,
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Added '{Tag}' tag to {MediaType} '{Title}' ({Year})",
            audienceTag,
            mediaType,
            SanitizeForLog(title),
            year);

        return true;
    }

    /// <summary>
    /// Processes a single movie to add audience tags.
    /// Retained for compatibility with existing callers and tests.
    /// </summary>
    /// <param name="movie">The movie to process.</param>
    /// <param name="aiService">The AI service to use.</param>
    /// <param name="overwriteExisting">
    /// Whether existing audience tags should be replaced.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessMovieAsync(
        Movie movie,
        IAiService aiService,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        await ProcessItemAsync(
                movie,
                aiService,
                overwriteExisting,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Processes a TV series once at the series level.
    /// Seasons and episodes are not processed individually.
    /// </summary>
    /// <param name="series">The TV series to process.</param>
    /// <param name="aiService">The AI service to use.</param>
    /// <param name="overwriteExisting">
    /// Whether existing audience tags should be replaced.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessSeriesAsync(
        Series series,
        IAiService aiService,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        await ProcessItemAsync(
                series,
                aiService,
                overwriteExisting,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
