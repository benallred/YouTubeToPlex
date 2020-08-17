using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;
using YouTubeToPlex.MediaServerHelpers;

using YTPlaylist = YoutubeExplode.Playlists.Playlist;

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

		private void ProcessVideos(IEnumerable<Video> videos, SeenItems seenItems, string downloadFolder, int season, LocalMetadata localMetadata)
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

				DownloadVideo(client, video, seasonFolder, videoFileNameBase, progress);
				DownloadAllCaptions(client, video, seasonFolder, videoFileNameBase, progress);
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

		private void DownloadVideo(YoutubeClient client, Video video, string downloadFolder, string videoFileNameBase, IProgress<double> progress)
		{
			var converter = new YoutubeConverter(client, Ffmpeg.DefaultFilePath);

			var mediaStreamInfoSet = client.Videos.Streams.GetManifestAsync(video.Id).Result;
			var videoStreamInfo = mediaStreamInfoSet.GetVideoOnly().OrderByDescending(info => info.VideoQuality).ThenByDescending(info => info.Framerate).First();
			var audioStreamInfo = mediaStreamInfoSet.GetAudioOnly().OrderByDescending(info => info.Bitrate).First();

			var extension = videoStreamInfo.Container.Name;

			converter.DownloadAndProcessMediaStreamsAsync(
					new IStreamInfo[] { videoStreamInfo, audioStreamInfo },
					Path.Combine(downloadFolder, videoFileNameBase + $".{extension}"),
					extension,
					progress)
				.Wait();
		}

		private void DownloadAllCaptions(YoutubeClient client, Video video, string downloadFolder, string videoFileNameBase, IProgress<double> progress)
		{
			var closedCaptionTrackInfos = client.Videos.ClosedCaptions.GetManifestAsync(video.Id).Result;

			DownloadCaptionsForLanguage(client, closedCaptionTrackInfos.Tracks, "en", downloadFolder, videoFileNameBase, progress);
			DownloadCaptionsForLanguage(client, closedCaptionTrackInfos.Tracks, "ko", downloadFolder, videoFileNameBase, progress);
		}

		private void DownloadCaptionsForLanguage(YoutubeClient client, IReadOnlyList<ClosedCaptionTrackInfo> closedCaptionTrackInfos, string languageCode, string downloadFolder, string videoFileNameBase, IProgress<double> progress)
		{
			closedCaptionTrackInfos
				.SingleOrDefault(info => !info.IsAutoGenerated && info.Language.Code == languageCode)
				.Do(closedCaptionTrackInfo =>
				{
					Console.WriteLine();
					Console.WriteLine($"  0.00% \t{closedCaptionTrackInfo.Language} captions");
					Console.SetCursorPosition(0, Console.CursorTop - 1);
					try
					{
						client.Videos.ClosedCaptions.DownloadAsync(closedCaptionTrackInfo,
								Path.Combine(downloadFolder, $"{videoFileNameBase}.{languageCode}.srt"),
								progress)
							.Wait();
					}
					catch (Exception ex)
					{
						Console.WriteLine();
						Console.Write("\t\tError downloading captions: " + ex.Message);
					}
				});
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

		private void CreateMetadata(Video video, string downloadFolder, string videoFileNameBase, LocalMetadata localMetadata)
		{
			Console.WriteLine();
			Console.Write("        \tmetadata");
			localMetadata.Save(new Episode(title: video.Title, plot: video.Description, aired: video.UploadDate), downloadFolder, videoFileNameBase);
		}

		private class PlaylistMetadataAndVideos
		{
			public YTPlaylist Metadata;
			public IReadOnlyList<Video> Videos;

			public PlaylistMetadataAndVideos(YTPlaylist metadata, IReadOnlyList<Video> videos)
			{
				Metadata = metadata;
				Videos = videos;
			}
		}
	}
}