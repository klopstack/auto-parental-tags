# Using repository.json to Add Auto Parental Tags Plugin to Jellyfin

This repository includes a `repository.json` manifest file that allows you to add the **Auto Parental Tags** plugin to your Jellyfin server via the plugin repositories feature.

## What is repository.json?

The `repository.json` file describes the Auto Parental Tags plugin, including its name, description, owner, and available versions. Jellyfin uses this manifest to discover and install plugins from custom sources.

## How to Add This Plugin Repository to Jellyfin

1. **Download or host the repository.json file**
   - You can use the raw file from this repository, or host it yourself on a web server.

2. **Open your Jellyfin server dashboard**
   - Go to: `https://<your-jellyfin-server>/web/#/dashboard/plugins/repositories`

3. **Add a new repository**
   - Click **Add Repository**.
   - For the URL, enter the direct link to your `repository.json` file. For example:
     - `https://raw.githubusercontent.com/klopstack/auto-parental-tags/master/repository.json`
     - Or your own hosted URL.

4. **Save and refresh repositories**
   - Click **Save**.
   - Click **Refresh** to update the plugin list.

5. **Install the Auto Parental Tags plugin**
   - Go to the **Catalog** tab in the Plugins section.
   - Search for **Auto Parental Tags**.
   - Click **Install** and follow the prompts.

## Notes
- You may need to restart Jellyfin after installing the plugin.
- Only use trusted sources for plugin repositories.
- For more information, see the [Jellyfin documentation on custom plugin repositories](https://jellyfin.org/docs/general/server/plugins/#custom-repositories).

---

**Auto Parental Tags**: Automatically tag movies with target audience (kids, teens, adults) using AI (Google Gemini, OpenAI, or LocalAI).
