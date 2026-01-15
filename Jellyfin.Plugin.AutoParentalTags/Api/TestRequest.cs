namespace Jellyfin.Plugin.AutoParentalTags.Api;

/// <summary>
/// Request model for running classification tests.
/// </summary>
public class TestRequest
{
    public string Provider { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? ModelName { get; set; }
    public string PromptTemplate { get; set; } = string.Empty;
}
