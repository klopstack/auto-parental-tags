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
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoParentalTags;

/// <summary>
/// Monitors library changes and processes movies.
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
            return;
        }

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            _logger.LogWarning("AI API key is not configured");
            return;
        }

        // Create the appropriate AI service
        using var aiService = _aiServiceFactory.CreateService(config);

        // Get all movies
        var movies = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie },
            IsVirtualItem = false,
            Recursive = true
        }).OfType<Movie>().ToList();

        _logger.LogInformation("Found {Count} movies to process", movies.Count);

        var processedCount = 0;
        var totalCount = movies.Count;

        foreach (var movie in movies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessMovieAsync(movie, aiService, config.OverwriteExistingTags, cancellationToken).ConfigureAwait(false);
                processedCount++;

                var progressPercent = (double)processedCount / totalCount * 100;
                progress.Report(progressPercent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing movie '{Title}': {Message}", movie.Name, ex.Message);
            }

            // Add a small delay to avoid rate limiting (configurable for testing)
            await Task.Delay(_processingDelay, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Completed processing {Count} movies", processedCount);
    }

    /// <summary>
    /// Processes a single movie to add audience tags.
    /// </summary>
    /// <param name="movie">The movie to process.</param>
    /// <param name="aiService">The AI service to use.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessMovieAsync(
        Movie movie,
        IAiService aiService,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        // Check if movie already has an audience tag
        var existingTags = movie.Tags?.Where(
            t => t.Equals("kids", StringComparison.OrdinalIgnoreCase)
                || t.Equals("teens", StringComparison.OrdinalIgnoreCase)
                || t.Equals("adults", StringComparison.OrdinalIgnoreCase)).ToList();

        if (existingTags?.Count > 0 && !overwriteExisting)
        {
            _logger.LogDebug(
                "Movie '{Title}' already has audience tag(s): {Tags}",
                movie.Name,
                string.Join(", ", existingTags));
            return;
        }

        // Get movie metadata
        var title = movie.Name;
        var year = movie.ProductionYear;
        var overview = movie.Overview;
        var rating = movie.OfficialRating;
        var genres = movie.Genres?.ToArray();

        // Call AI API
        var audienceTag = await aiService.DetermineTargetAudienceAsync(
            title,
            year,
            overview,
            rating,
            genres).ConfigureAwait(false);

        if (string.IsNullOrEmpty(audienceTag))
        {
            _logger.LogWarning("Could not determine audience for '{Title}'", title);
            return;
        }

        // Remove old audience tags if overwriting
        if (overwriteExisting && existingTags?.Count > 0)
        {
            var tagsList = movie.Tags?.ToList() ?? new List<string>();
            foreach (var tag in existingTags)
            {
                tagsList.Remove(tag);
            }

            movie.Tags = tagsList.ToArray();
        }

        // Add the new tag
        var currentTags = movie.Tags?.ToList() ?? new List<string>();
        if (!currentTags.Contains(audienceTag, StringComparer.OrdinalIgnoreCase))
        {
            currentTags.Add(audienceTag);
            movie.Tags = currentTags.ToArray();

            // Save changes
            await movie.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Added '{Tag}' tag to '{Title}' ({Year})",
                audienceTag,
                title,
                year);
        }
    }
}
