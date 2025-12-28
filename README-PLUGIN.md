# Auto Parental Tags

![coverage](https://img.shields.io/badge/coverage-80%25-brightgreen)

A Jellyfin plugin that uses AI to analyze movie metadata and automatically add audience target tags (kids, teens, adults).

## Features

- **Multiple AI Provider Support**: Choose from Google Gemini, OpenAI, or LocalAI
- Automatically analyzes movies using AI
- Adds one of three audience tags: `kids`, `teens`, or `adults`
- Considers target audience rather than just content ratings
- Recognizes that historical context matters (e.g., pre-1990 PG films often targeted adults)
- Processes movies during library scans or on-demand
- Configurable to overwrite or preserve existing tags

## Supported AI Providers

### Google Gemini
- Free tier available
- Good performance
- Get API key at https://makersuite.google.com/app/apikey

### OpenAI
- GPT-3.5-turbo or GPT-4
- Paid service
- Get API key at https://platform.openai.com/api-keys

### LocalAI
- Self-hosted, privacy-focused
- OpenAI-compatible API
- Run locally on your own hardware
- No API costs after setup
- Learn more at https://localai.io

## Installation

1. Build the plugin using the provided build task
2. Copy the built DLL to your Jellyfin plugins directory: `[jellyfin-data-dir]/plugins/Jellyfin.Plugin.AutoParentalTags/`
3. Restart Jellyfin
4. Configure the plugin with your preferred AI provider

## Building

```bash
dotnet publish --configuration=Debug Jellyfin.Plugin.AutoParentalTags.sln
```

Or use the VS Code task: `build-and-copy`

## Configuration

1. Navigate to Dashboard → Plugins → Auto Parental Tags
2. Select your AI Provider:
   - **Google Gemini**: Enter your Gemini API key
   - **OpenAI**: Enter your OpenAI API key and optionally customize the model
   - **LocalAI**: Enter your LocalAI endpoint URL and model name
3. Configure settings:
   - **Enable Automatic Tagging**: Turn the plugin on/off
   - **Process on Library Scan**: Automatically process new movies during library scans
   - **Overwrite Existing Tags**: Replace existing audience tags when processing

### LocalAI Setup Example

If you're running LocalAI locally:
1. Set Provider to "LocalAI / Custom Endpoint"
2. Set API Endpoint to `http://localhost:8080` (or your LocalAI URL)
3. Set Model Name to your installed model (e.g., `gpt-3.5-turbo`, `llama2`, etc.)
4. Leave API Key blank if your LocalAI instance doesn't require authentication

## How It Works

The plugin analyzes each movie using:
- Title and release year
- Overview/synopsis
- Official MPAA rating (if available)
- Genres

It then asks the AI to determine the **target audience** (not content rating), considering:
- The film's marketing and intended demographic
- Themes and subject matter complexity
- Historical context
- Franchise targeting
- Storytelling sophistication

## Examples

- **Charlie Brown Christmas** (NR) → `kids` - Clearly targeted at children despite being unrated
- **James Bond** (PG) → `adults` - Marketed to adults despite PG rating
- **Star Wars** (PG/PG-13) → `kids` or `teens` - Depending on the specific film
- **The Godfather** (R) → `adults` - Clearly adult-oriented content

## Requirements

- Jellyfin 10.9.x or higher
- .NET 9.0 runtime
- One of:
  - Google Gemini API key
  - OpenAI API key
  - LocalAI installation

## Privacy & Costs

- **Google Gemini**: Free tier available, data sent to Google
- **OpenAI**: Paid per request, data sent to OpenAI
- **LocalAI**: Free after setup, all processing stays on your hardware (most private option)

## License

See [LICENSE](LICENSE) for details.
