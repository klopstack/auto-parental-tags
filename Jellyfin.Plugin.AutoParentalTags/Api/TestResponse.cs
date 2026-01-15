namespace Jellyfin.Plugin.AutoParentalTags.Api;

/// <summary>
/// Response object for classification tests.
/// </summary>
public class TestResponse
{
    /// <summary>
    /// Gets the collection of individual test results.
    /// </summary>
    public System.Collections.ObjectModel.Collection<TestResult> Results { get; } = new System.Collections.ObjectModel.Collection<TestResult>();
}
