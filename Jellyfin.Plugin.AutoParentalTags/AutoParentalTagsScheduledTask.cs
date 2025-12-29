using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoParentalTags.Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoParentalTags;

/// <summary>
/// Scheduled task for manually running Auto Parental Tags.
/// </summary>
public class AutoParentalTagsScheduledTask : IScheduledTask
{
    private readonly LibraryMonitor _libraryMonitor;
    private readonly ILogger<AutoParentalTagsScheduledTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoParentalTagsScheduledTask"/> class.
    /// </summary>
    /// <param name="libraryMonitor">Instance of the <see cref="LibraryMonitor"/> class.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{AutoParentalTagsScheduledTask}"/> interface.</param>
    public AutoParentalTagsScheduledTask(
        LibraryMonitor libraryMonitor,
        ILogger<AutoParentalTagsScheduledTask> logger)
    {
        _libraryMonitor = libraryMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Auto Parental Tags";

    /// <inheritdoc />
    public string Key => "AutoParentalTags";

    /// <inheritdoc />
    public string Description => "Analyzes movies and adds target audience tags (kids, teens, adults) using AI.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual Auto Parental Tags task started");

        try
        {
            await _libraryMonitor.Run(progress, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Manual Auto Parental Tags task completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Auto Parental Tags task: {Message}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // No default triggers - user must manually run
        return Array.Empty<TaskTriggerInfo>();
    }
}
