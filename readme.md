# YouTubeToPlex

Downloads videos from YouTube and creates metadata for use in Plex (via something like [XBMCnfoTVImporter](https://github.com/gboudreau/XBMCnfoTVImporter.bundle)), Kodi, or other media players that support .nfo files.

## Requirements

[.NET Core 3](https://dotnet.microsoft.com/download)

## Usage

Current functionality is limited to downloading all videos in a playlist.

`dotnet run --project .\YouTubeToPlex\ -- --playlist-id <playlist id> --download-folder <download folder>`
