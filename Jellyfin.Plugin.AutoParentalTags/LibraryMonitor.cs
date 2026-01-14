using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryMonitor> _logger;
    private readonly AiServiceFactory _aiServiceFactory;
    private readonly TimeSpan _processingDelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryMonitor"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{LibraryMonitor}"/> interface.</param>
    /// <param name="aiServiceFactory">Instance of the <see cref="AiServiceFactory"/> class.</param>
    /// <param name="processingDelay">Optional delay between processing movies.</param>
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

        return value.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        PluginConfiguration? config;
        try
        {
            config = Plugin.Instance?.Configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load plugin configuration");
            return;
        }

        if (config == null || !config.EnableAutoTagging || !config.ProcessOnLibraryScan)
        {
            _logger.LogDebug("Auto-tagging is disabled or not configured to run on library scan");
            progress?.Report(100);
            return;
        }

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            _logger.LogWarning("AI API key is not configured");
            progress?.Report(100);
            return;
        }

        // Create the appropriate AI service
        using var aiService = _aiServiceFactory.CreateService(config);

        // Get all movies and series from active library folders
        var allItems = new List<BaseItem>();

        foreach (var library in _libraryManager.RootFolder.Children)
        {
            var libraryItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                IsPlaceHolder = false,
                Parent = library,
                Recursive = true
            });

            allItems.AddRange(libraryItems.Where(i => i is Movie or Series));
        }

        _logger.LogInformation(
            "Found {Count} items to process ({Movies} movies, {Series} series)",
            allItems.Count,
            allItems.OfType<Movie>().Count(),
            allItems.OfType<Series>().Count());

        var processedCount = 0;
        var totalCount = allItems.Count;

        foreach (var item in allItems)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessItemAsync(item, aiService, config.OverwriteExistingTags, cancellationToken).ConfigureAwait(false);
                processedCount++;

                var progressPercent = (double)processedCount / totalCount * 100;
                progress.Report(progressPercent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item '{Title}': {Message}", SanitizeForLog(item.Name), ex.Message);
            }

            // Add a small delay to avoid rate limiting (configurable for testing)
            await Task.Delay(_processingDelay, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Completed processing {Count} items", processedCount);

        // Always report 100% completion at the end
        progress?.Report(100);
    }

    /// <summary>
    /// Processes a single item (movie or series) to add audience tags.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <param name="aiService">The AI service to use.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessItemAsync(
        BaseItem item,
        IAiService aiService,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        // Check if item already has an audience tag
        var existingTags = item.Tags?.Where(
            t => t.Equals("kids", StringComparison.OrdinalIgnoreCase)
                || t.Equals("teens", StringComparison.OrdinalIgnoreCase)
                || t.Equals("family", StringComparison.OrdinalIgnoreCase)
                || t.Equals("adults", StringComparison.OrdinalIgnoreCase)).ToList();

        if (existingTags?.Count > 0 && !overwriteExisting)
        {
            _logger.LogDebug(
                "{ItemType} '{Title}' already has audience tag(s): {Tags}",
                item is Movie ? "Movie" : "Series",
                item.Name,
                string.Join(", ", existingTags));
            return;
        }

        // Get item metadata
        var title = item.Name;
        var year = item.ProductionYear;
        var overview = item.Overview;
        var rating = item.OfficialRating;
        var genres = item.Genres?.ToArray();
        var studios = item.Studios?.ToArray();

        // Get non-audience tags to pass to AI
        var nonAudienceTags = item.Tags?.Where(t =>
            !t.Equals("kids", StringComparison.OrdinalIgnoreCase) &&
            !t.Equals("teens", StringComparison.OrdinalIgnoreCase) &&
            !t.Equals("family", StringComparison.OrdinalIgnoreCase) &&
            !t.Equals("adults", StringComparison.OrdinalIgnoreCase)).ToArray();

        // Call AI API
        var audienceTag = await aiService.DetermineTargetAudienceAsync(
            title,
            year,
            overview,
            rating,
            genres,
            nonAudienceTags,
            studios).ConfigureAwait(false);

        if (string.IsNullOrEmpty(audienceTag))
        {
            _logger.LogWarning("Could not determine audience for '{Title}'", SanitizeForLog(title));
            return;
        }

        // Remove old audience tags if overwriting
        if (overwriteExisting && existingTags?.Count > 0)
        {
            var tagsList = item.Tags?.ToList() ?? new List<string>();
            foreach (var tag in existingTags)
            {
                tagsList.Remove(tag);
            }

            item.Tags = tagsList.ToArray();
        }

        // Add the new tag
        var currentTags = item.Tags?.ToList() ?? new List<string>();
        if (!currentTags.Contains(audienceTag, StringComparer.OrdinalIgnoreCase))
        {
            currentTags.Add(audienceTag);
            item.Tags = currentTags.ToArray();

            // Save changes
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Added '{Tag}' tag to {ItemType} '{Title}' ({Year})",
                audienceTag,
                item is Movie ? "movie" : "series",
                SanitizeForLog(title),
                year);
        }
    }
}
