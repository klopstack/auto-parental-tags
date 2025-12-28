# Auto Parental Tags

![coverage](https://img.shields.io/badge/coverage-80%25-brightgreen)
[![CI](https://github.com/benklop/auto-parental-tags/actions/workflows/build-test-coverage.yaml/badge.svg)](https://github.com/benklop/auto-parental-tags/actions/workflows/build-test-coverage.yaml)

A Jellyfin plugin that uses AI to analyze movie metadata and automatically add audience target tags (kids, teens, adults).

## Features

- **Multiple AI Provider Support**: Choose from Google Gemini, OpenAI, or LocalAI
- Automatically analyzes movies using AI to determine target audience
- Adds one of three audience tags: `kids`, `teens`, or `adults`
- Considers target audience rather than just content ratings
- Recognizes that historical context matters (e.g., pre-1990 PG films often targeted adults)
- Processes movies during library scans or on-demand
- Configurable to overwrite or preserve existing tags
- Privacy-focused option with LocalAI (self-hosted, no external API calls)

## Supported AI Providers

### Google Gemini
- Free tier available with generous limits
- Fast and reliable performance
- Get API key at https://makersuite.google.com/app/apikey

### OpenAI
- GPT-3.5-turbo or GPT-4 models
- Paid service (per-token pricing)
- Get API key at https://platform.openai.com/api-keys

### LocalAI
- Self-hosted, privacy-focused solution
- OpenAI-compatible API
- Run locally on your own hardware
- No API costs after initial setup
- Complete data privacy
- Learn more at https://localai.io

## Installation

### From Release (Recommended)

1. Download the latest release from the [Releases page](https://github.com/benklop/auto-parental-tags/releases)
2. Extract the zip file
3. Copy the DLL to your Jellyfin plugins directory:
   - Linux: `/var/lib/jellyfin/plugins/AutoParentalTags/`
   - Windows: `C:\ProgramData\Jellyfin\Server\plugins\AutoParentalTags\`
   - Docker: `/config/plugins/AutoParentalTags/`
4. Restart Jellyfin
5. Configure the plugin in Dashboard → Plugins → Auto Parental Tags

### Building from Source

```bash
# Clone the repository
git clone https://github.com/benklop/auto-parental-tags.git
cd auto-parental-tags

# Build the plugin
dotnet publish --configuration=Release Jellyfin.Plugin.AutoParentalTags.sln

# The built DLL will be in:
# Jellyfin.Plugin.AutoParentalTags/bin/Release/net9.0/publish/
```

#### VS Code Task (Development)

Use the `build-and-copy` task to automatically build and copy to your local Jellyfin instance.

## Configuration

1. Navigate to **Dashboard** → **Plugins** → **Auto Parental Tags**
2. Select your **AI Provider**:
   - **Google Gemini**: Enter your Gemini API key
   - **OpenAI**: Enter your OpenAI API key and optionally customize the model name
   - **LocalAI**: Enter your LocalAI endpoint URL and model name
3. Configure settings:
   - **Enable Automatic Tagging**: Turn the plugin on/off globally
   - **Process on Library Scan**: Automatically process new movies during library scans
   - **Overwrite Existing Tags**: Replace existing audience tags when re-processing

### LocalAI Setup Example

If you're running LocalAI locally:

1. Set **Provider** to "LocalAI / Custom Endpoint"
2. Set **API Endpoint** to `http://localhost:8080/v1/chat/completions` (or your LocalAI URL)
3. Set **Model Name** to your installed model (e.g., `gpt-3.5-turbo`, `llama2`, etc.)
4. Leave **API Key** blank if your LocalAI instance doesn't require authentication
5. Click **Save**

## How It Works

The plugin analyzes each movie using:
- **Title** and **release year**
- **Overview/synopsis**
- **Official MPAA rating** (if available)
- **Genres**

It then asks the AI to determine the **target audience** (not just content rating), considering:
- The film's marketing and intended demographic
- Themes and subject matter complexity
- Historical context (e.g., pre-1990 PG ratings)
- Franchise targeting patterns
- Storytelling sophistication level

### Tagging Examples

| Movie | Rating | Tag | Reasoning |
|-------|--------|-----|-----------|
| Charlie Brown Christmas | NR | `kids` | Clearly targeted at children despite being unrated |
| James Bond (early films) | PG | `adults` | Marketed to adults despite PG rating |
| Star Wars (original trilogy) | PG | `kids` | Family-friendly adventure targeted at younger audiences |
| The Empire Strikes Back | PG | `teens` | Darker themes, more complex storytelling |
| The Godfather | R | `adults` | Clearly adult-oriented content and themes |
| Toy Story | G | `kids` | Animated family film for young audiences |

## Requirements

- **Jellyfin**: 10.9.x or higher
- **.NET Runtime**: 9.0
- **AI Provider** (one of):
  - Google Gemini API key (free tier available)
  - OpenAI API key (paid)
  - LocalAI installation (self-hosted)

## Development

### Project Structure

```
auto-parental-tags/
├── Jellyfin.Plugin.AutoParentalTags/       # Main plugin project
│   ├── Configuration/                      # Plugin configuration
│   ├── Services/                           # AI service implementations
│   │   ├── IAiService.cs                   # AI service interface
│   │   ├── AiServiceFactory.cs             # Factory for creating services
│   │   ├── GeminiService.cs                # Google Gemini implementation
│   │   └── OpenAiService.cs                # OpenAI/LocalAI implementation
│   ├── LibraryMonitor.cs                   # Library scan integration
│   ├── Plugin.cs                           # Plugin entry point
│   └── ServiceRegistrator.cs               # DI registration
├── Jellyfin.Plugin.AutoParentalTags.Tests/ # Unit tests
└── .github/workflows/                      # CI/CD workflows
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# View coverage report (80%+ line coverage maintained)
```

### Code Quality

- **Linting**: `dotnet format` (automatically enforced in CI)
- **Testing**: xUnit with Moq for mocking
- **Coverage**: 80%+ line coverage with Coverlet
- **CI/CD**: GitHub Actions for build, test, lint, and coverage validation

### Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Ensure tests pass (`dotnet test`)
5. Ensure code is formatted (`dotnet format`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## Privacy & Data Considerations

### Data Sent to AI Services

When using external AI providers (Gemini, OpenAI), the following movie metadata is sent:
- Movie title
- Release year
- Overview/synopsis text
- MPAA rating
- Genre list

**No personally identifiable information, viewing history, or file paths are transmitted.**

### Privacy Options

- **Most Private**: LocalAI (all processing on your hardware, no external calls)
- **Moderate**: OpenAI/Gemini with minimal metadata (disable synopsis in future versions)
- **Consider**: Rate limiting and batch processing to minimize API calls

## Troubleshooting

### Plugin doesn't appear in dashboard
- Verify the DLL is in the correct plugins directory
- Restart Jellyfin completely
- Check Jellyfin logs for plugin loading errors

### Movies aren't being tagged
- Verify the plugin is enabled in configuration
- Check that "Process on Library Scan" is enabled
- Ensure your API key is valid and has quota remaining
- Check Jellyfin logs for API errors or rate limiting

### Tags are incorrect
- Try "Overwrite Existing Tags" to re-process with improved prompts
- Different AI models may give different results
- Consider switching providers if results are consistently poor

### LocalAI not working
- Verify the endpoint URL is correct and includes `/v1/chat/completions`
- Ensure LocalAI is running and accessible from Jellyfin server
- Check that the model name matches an installed LocalAI model
- Review LocalAI logs for request/response details

## Roadmap

- [ ] Support for TV shows and series
- [ ] Manual tagging interface in web UI
- [ ] Batch re-tagging of library subsets
- [ ] Custom tag names/categories
- [ ] Multi-language support for AI prompts
- [ ] Integration with Jellyfin Smart Filters
- [ ] Caching/database of previous AI decisions

## License

This project is licensed under the terms specified in [LICENSE](LICENSE).

## Acknowledgments

- [Jellyfin](https://jellyfin.org/) - The amazing free media server
- [LocalAI](https://localai.io/) - Privacy-focused local AI inference
- Plugin template and build infrastructure from [jellyfin-plugin-template](https://github.com/jellyfin/jellyfin-plugin-template)

## Support

- **Issues**: [GitHub Issues](https://github.com/benklop/auto-parental-tags/issues)
- **Discussions**: [GitHub Discussions](https://github.com/benklop/auto-parental-tags/discussions)
- **Jellyfin Forum**: [Plugins section](https://forum.jellyfin.org/)
