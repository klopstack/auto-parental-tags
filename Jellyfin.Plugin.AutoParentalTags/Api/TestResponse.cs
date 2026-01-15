namespace Jellyfin.Plugin.AutoParentalTags.Api;

public class TestResult
{
    public string Title { get; set; } = string.Empty;
    public string Expected { get; set; } = string.Empty;
    public string? Actual { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class TestResponse
{
    public TestResult[] Results { get; set; } = System.Array.Empty<TestResult>();
}
