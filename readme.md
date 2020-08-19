# YouTubeToPlex

Downloads videos from YouTube and creates metadata for use in Plex (via something like [XBMCnfoTVImporter](https://github.com/gboudreau/XBMCnfoTVImporter.bundle)), Kodi, or other media players that support .nfo files.

## Requirements

[.NET Core 3](https://dotnet.microsoft.com/download)

## Usage

`dotnet run --project .\YouTubeToPlex\ -- --help`

### Download playlist as TV show

`dotnet run --project .\YouTubeToPlex\ -- playlist --id <playlist id> --download-folder <download folder>`

By default, the playlist is ordered by upload date. This can be disabled by using `--do-not-reorder`

`dotnet run --project .\YouTubeToPlex\ -- playlist --id <playlist id> --download-folder <download folder> --do-not-reorder`

By default, videos are downloaded to the "Season 1" folder. This can be overridden by using `--season <number>`

`dotnet run --project .\YouTubeToPlex\ -- playlist --id <playlist id> --download-folder <download folder> --season <number>`

Example:

`dotnet run --project .\YouTubeToPlex\ -- playlist --id PLGVpxD1HlmJ-bs-pAN2wH8ykEXs8YoW6D --do-not-reorder --download-folder "C:\Media\TV\Studio C" --season 2`

### Download single video as movie

`dotnet run --project .\YouTubeToPlex\ -- video --id <video id> --download-folder <download folder>`

Example:

`dotnet run --project .\YouTubeToPlex\ -- video --id 7hhcURZVx5k --download-folder "C:\Media\Movies\Handel's Messiah - Easter Concert with The Tabernacle Choir"`
