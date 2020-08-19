using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YouTubeToPlex.MediaServerHelpers;

using YTPlaylist = YoutubeExplode.Playlists.Playlist;
using YTVideo = YoutubeExplode.Videos.Video;

namespace YouTubeToPlex.SubPrograms.Playlist
{
	internal class PlaylistSubProgram : ISubProgram
	{
		private HttpClient HttpClient { get; }

		public PlaylistSubProgram(HttpClient httpClient)
		{
			HttpClient = httpClient;
		}

		public Command GetCommand()
		{
			var command = new Command("playlist", "Downloads videos from a YouTube playlist")
			{
				new Option<string>("--id", "The ID of the YouTube playlist"),
				new Option<bool>("--do-not-reorder", "If true, the default playlist order is used. If false, the playlist is ordered by upload date."),
				new Option<string>("--download-folder", "The folder to download videos to"),
				new Option<int>("--season", "The season folder to use [default = 1]"),
			};
			command.Handler = CommandHandler.Create<string, bool, string, int>(DownloadPlaylist);
			return command;
		}

		public void DownloadPlaylist(string id, bool doNotReorder, string downloadFolder, int season = 1)
		{
			if (id == null) throw new ArgumentNullException(nameof(id));
			if (downloadFolder == null) throw new ArgumentNullException(nameof(downloadFolder));

			Directory.CreateDirectory(downloadFolder);

			var seenItems = new SeenItems(downloadFolder);
			var localMetadata = new LocalMetadata(HttpClient);

			var playlist = GetPlaylist(id);
			EnsureMetadata(playlist, downloadFolder, localMetadata);

			var allVideos = playlist.Videos;
			var sortedVideos = doNotReorder ? allVideos : allVideos.OrderBy(item => item.UploadDate).ToList();
			var newVideos = sortedVideos.Where(video => !seenItems.GetIds().Contains(video.Id.Value));
			ProcessVideos(newVideos, seenItems, downloadFolder, season, localMetadata);
		}

		private PlaylistMetadataAndVideos GetPlaylist(string playlistId)
		{
			Console.WriteLine($"Getting playlist {playlistId}");
			var client = new YoutubeClient();
			var playlist = client.Playlists.GetAsync(playlistId);
			var videos = client.Playlists.GetVideosAsync(playlistId);
			return new PlaylistMetadataAndVideos(playlist.Result, videos.BufferAsync().Result);
		}

		private void ProcessVideos(IEnumerable<YTVideo> videos, SeenItems seenItems, string downloadFolder, int season, LocalMetadata localMetadata)
		{
			var client = new YoutubeClient();
			var seasonFolder = Path.Combine(downloadFolder, $"Season {season}");
			Directory.CreateDirectory(seasonFolder);
			var episodeNumber = GetLastEpisodeNumber(seasonFolder);
			videos.ToList().ForEach(video =>
			{
				episodeNumber++;

				Console.WriteLine($"  0.00% Downloading {video.Id} {episodeNumber} {video.Title}");
				Console.SetCursorPosition(0, Console.CursorTop - 1);

				var videoFileNameBase = $"S{season.ToString().PadLeft(2, '0')}E{episodeNumber.ToString("N0").PadLeft(2, '0')} " + video.Title.Aggregate("", (agg, cur) => Path.GetInvalidFileNameChars().Contains(cur) ? agg : agg + cur);

				var progress = new ConcurrentProgress<double>(d =>
				{
					Console.SetCursorPosition(0, Console.CursorTop);
					Console.Write((d * 100).ToString("N2").PadLeft(6));
				});

				Downloaders.DownloadVideo(client, video, seasonFolder, videoFileNameBase, progress);
				Downloaders.DownloadAllCaptions(client, video, seasonFolder, videoFileNameBase, progress);
				CreateMetadata(video, seasonFolder, videoFileNameBase, localMetadata);

				seenItems.SaveId(video.Id);

				Console.WriteLine();
			});
		}

		private int GetLastEpisodeNumber(string seasonFolder)
		{
			return Directory
				.EnumerateFiles(seasonFolder, "S??E??*")
				.Select(fileName => Regex.Match(fileName, @"S\d\dE(\d{2,}) ").Groups[1].Value.Convert(int.Parse))
				.DefaultIfEmpty()
				.Max();
		}

		private void EnsureMetadata(PlaylistMetadataAndVideos playlist, string downloadFolder, LocalMetadata localMetadata)
		{
			if (!File.Exists(Path.Combine(downloadFolder, "tvshow.nfo")))
			{
				Console.WriteLine("Creating metadata");
				Console.Write("\tInput poster path or URL: ");
				var posterPathOrUrl = Console.ReadLine();
				localMetadata.Save(
					new TVShow(
						title: playlist.Metadata.Title,
						plot: playlist.Metadata.Description,
						premiered: playlist.Videos.OrderBy(video => video.UploadDate).First().UploadDate,
						posterPathOrUrl: posterPathOrUrl),
					downloadFolder);
			}
		}

		private void CreateMetadata(YTVideo video, string downloadFolder, string videoFileNameBase, LocalMetadata localMetadata)
		{
			Console.WriteLine();
			Console.Write("        \tmetadata");
			localMetadata.Save(new Episode(title: video.Title, plot: video.Description, aired: video.UploadDate), downloadFolder, videoFileNameBase);
		}

		private class PlaylistMetadataAndVideos
		{
			public YTPlaylist Metadata;
			public IReadOnlyList<YTVideo> Videos;

			public PlaylistMetadataAndVideos(YTPlaylist metadata, IReadOnlyList<YTVideo> videos)
			{
				Metadata = metadata;
				Videos = videos;
			}
		}
	}
}
