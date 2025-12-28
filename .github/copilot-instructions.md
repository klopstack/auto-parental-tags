# Copilot Instructions for Auto Parental Tags

This is a **Jellyfin plugin** (C# .NET 9.0) that uses AI services to automatically tag movies with target audience levels (kids, teens, adults).

## Architecture Overview

### Core Components

1. **Plugin Entry Point** ([Plugin.cs](../Jellyfin.Plugin.AutoParentalTags/Plugin.cs))
   - Extends `BasePlugin<PluginConfiguration>` and `IHasWebPages`
   - Provides configuration page served as embedded HTML resource
   - Singleton instance accessible via `Plugin.Instance`

2. **Library Monitoring** ([LibraryMonitor.cs](../Jellyfin.Plugin.AutoParentalTags/LibraryMonitor.cs))
   - Implements `ILibraryPostScanTask` - runs after Jellyfin library scans
   - Queries movies from `ILibraryManager` and applies audience tags
   - Uses configurable `ProcessOnLibraryScan` and `OverwriteExistingTags` settings

3. **AI Service Abstraction** ([Services/IAiService.cs](../Jellyfin.Plugin.AutoParentalTags/Services/IAiService.cs))
   - Unified interface for multiple AI providers
   - Core method: `DetermineTargetAudienceAsync()` - takes title, year, overview, rating, genres → returns "kids"/"teens"/"adults"
   - Implementations: `GeminiService`, `OpenAiService` (handles both OpenAI and LocalAI endpoints)

4. **Service Factory** ([Services/AiServiceFactory.cs](../Jellyfin.Plugin.AutoParentalTags/Services/AiServiceFactory.cs))
   - Creates appropriate `IAiService` based on `PluginConfiguration.Provider` enum
   - Configures service with API keys and endpoints before returning
   - LocalAI and OpenAI both use `OpenAiService` with different endpoints

5. **Dependency Injection** ([ServiceRegistrator.cs](../Jellyfin.Plugin.AutoParentalTags/ServiceRegistrator.cs))
   - Implements `IPluginServiceRegistrator`
   - Registers singletons: `AiServiceFactory` and `LibraryMonitor`
   - Jellyfin's DI container automatically calls this on plugin load

### Configuration System

[PluginConfiguration.cs](../Jellyfin.Plugin.AutoParentalTags/Configuration/PluginConfiguration.cs) properties:
- `Provider`: Enum (Gemini, OpenAI, LocalAI) - determines which service to instantiate
- `ApiKey`: Credentials for Gemini/OpenAI (empty for LocalAI unless required)
- `ApiEndpoint`: Only used by LocalAI and OpenAI (default: `http://localhost:8080`)
- `ModelName`: For OpenAI/LocalAI only (e.g., `gpt-3.5-turbo`, `llama2`)
- Feature toggles: `EnableAutoTagging`, `ProcessOnLibraryScan`, `OverwriteExistingTags`

The configuration page HTML is served via embedded resource path pattern in `Plugin.GetPages()`.

## Development Workflows

### Build & Deploy

**VS Code Tasks** (defined in workspace settings):
- `build`: Runs `dotnet publish --configuration=Debug` to output DLL to `bin/Debug/net9.0/publish/`
- `make-plugin-dir`: Creates Jellyfin plugin directory (uses config: `jellyfinLinuxDataDir`)
- `copy-dll`: Copies DLL to active Jellyfin instance
- `build-and-copy`: Composite task running all three sequentially

**Manual commands:**
```bash
dotnet publish --configuration=Release Jellyfin.Plugin.AutoParentalTags.sln
# Output: Jellyfin.Plugin.AutoParentalTags/bin/Release/net9.0/publish/
```

Plugin location after build:
- Linux: `$HOME/.local/share/jellyfin/plugins/Jellyfin.Plugin.AutoParentalTags/`
- Windows: `%LOCALAPPDATA%/jellyfin/plugins/Jellyfin.Plugin.AutoParentalTags/`

### Creating Releases

**Version Format Fix**: This project uses a local copy of the changelog workflow ([.github/workflows/changelog-local.yaml](../.github/workflows/changelog-local.yaml)) that fixes the upstream version formatting bug. The fixed workflow properly formats versions as:
- `<Version>X.Y.Z</Version>` (3 segments for NuGet)
- `<AssemblyVersion>A.B.C.D</AssemblyVersion>` (4 segments, matches targetAbi from build.yaml)
- `<FileVersion>X.Y.Z.0</FileVersion>` (4 segments)

**Release Process:**
1. **Merge PRs** to `master` with proper labels:
   - `semver: major` - Breaking changes (1.0.0 → 2.0.0)
   - `semver: minor` - New features (1.0.0 → 1.1.0)
   - `semver: patch` - Bug fixes (1.0.0 → 1.0.1, default)
2. **Release Drafter** auto-creates a draft release with changelog
3. **Changelog workflow** creates `prepare-X.Y.Z` PR with updated versions
4. **Review and merge** the prepare PR (version formats are now correct)
5. **Publish** the draft release on GitHub
6. **Automated deployment** triggers via [publish.yaml](../.github/workflows/publish.yaml)

### Testing

Solution includes [Jellyfin.Plugin.AutoParentalTags.Tests](../Jellyfin.Plugin.AutoParentalTags.Tests/) project with:
- Unit tests for `LibraryMonitor`, `Plugin`, `ServiceRegistrator`
- Service tests for `GeminiService`, `OpenAiService`, `AiServiceFactory`
- Configuration tests for `PluginConfiguration`

Run: `dotnet test` or use VS Code test explorer

## Key Patterns & Conventions

### Async Movie Processing
`LibraryMonitor.Run()` is async and receives a `CancellationToken`. Always respect cancellation for responsive shutdown. Uses configurable `_processingDelay` (default 1s) between movies to avoid overwhelming external APIs.

### Tag Management
Movies hold tags via `MediaBrowser.Model.Entities.PersonKind` enum in Jellyfin's object model. Tags are set conditionally:
- If `OverwriteExistingTags=true`: Replace any existing audience tag
- Otherwise: Skip movies that already have kids/teens/adults tags
- Only set if AI returns a recognized value (case-sensitive)

### HTTP Clients in Services
- `GeminiService`: Uses instance `HttpClient` field (initialized in constructor)
- `OpenAiService`: Similar pattern
- Both should implement `IDisposable` properly to clean up clients

### Nullable Annotations
Codebase uses C# nullable reference types throughout. `string?`, `int?`, `string[]?` are common in API signatures to handle optional movie metadata.

## External Dependencies

- **Jellyfin SDK** ([MediaBrowser.Controller](https://github.com/jellyfin/jellyfin)): Core server APIs
- **Google Gemini API**: REST endpoint at `https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent`
- **OpenAI API**: REST endpoint at `https://api.openai.com/v1/chat/completions` (or custom for LocalAI)
- **.NET HttpClient**: For all HTTP calls to external AI services

## Common Tasks

**Adding a new AI provider:**
1. Create `Services/YourNewService.cs` implementing `IAiService`
2. Add enum variant to `AiProvider` enum in `PluginConfiguration.cs`
3. Update `AiServiceFactory.CreateService()` switch statement
4. Update configuration HTML form to expose new provider option

**Modifying movie processing logic:**
- Edit `LibraryMonitor.Run()` - main entry point called after each library scan
- Ensure cancellation token is checked in loops
- Log appropriately with `_logger.LogInformation()`, `LogWarning()`, etc.

**Debugging configuration issues:**
- Plugin reads config from Jellyfin's built-in configuration system
- Access current config via `Plugin.Instance?.Configuration`
- Configuration changes trigger Jellyfin to call plugin methods; doesn't auto-reload in background tasks

## File Structure

- `Plugin.cs` - Main plugin class
- `LibraryMonitor.cs` - Library scan integration
- `ServiceRegistrator.cs` - DI setup
- `Configuration/` - Config schema and UI
- `Services/` - AI provider implementations (IAiService, GeminiService, OpenAiService, AiServiceFactory)
- `*.Tests/` - Corresponding test files mirror this structure
