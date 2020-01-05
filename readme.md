# YouTubeToPlex

Downloads videos from YouTube and creates metadata for use in Plex (via something like [XBMCnfoTVImporter](https://github.com/gboudreau/XBMCnfoTVImporter.bundle)), Kodi, or other media players that support .nfo files.

## Requirements

[.NET Core 3](https://dotnet.microsoft.com/download)

## Usage

`dotnet run --project .\YouTubeToPlex\ -- --usage`

Current functionality is limited to downloading all videos in a playlist.

`dotnet run --project .\YouTubeToPlex\ -- --playlist-id <playlist id> --download-folder <download folder>`

By default, the playlist is ordered by upload date. This can be disabled by using `--do-not-reorder`

`dotnet run --project .\YouTubeToPlex\ -- --playlist-id <playlist id> --download-folder <download folder> --do-not-reorder`
