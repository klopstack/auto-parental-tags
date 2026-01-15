namespace Jellyfin.Plugin.AutoParentalTags.Api;

/// <summary>
/// Result for a single test case.
/// </summary>
public class TestResult
{
    /// <summary>
    /// Gets or sets the test title (example movie title).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected audience label.
    /// </summary>
    public string Expected { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actual audience label returned by the AI (if available).
    /// </summary>
    public string? Actual { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the actual label matched the expected label.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the test failed due to an exception.
    /// </summary>
    public string? Error { get; set; }
}
