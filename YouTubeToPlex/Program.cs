using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;
using YouTubeToPlex.MediaServerHelpers;

namespace YouTubeToPlex
{
	internal class Program
	{
		/// <summary>
		/// Downloads videos from a YouTube playlist and creates metadata for use in media players.
		/// </summary>
		/// <param name="playlistId">The ID of the YouTube playlist.</param>
		/// <param name="doNotReorder">If true, the default playlist order is used. If false, the playlist is ordered by upload date.</param>
		/// <param name="downloadFolder">The folder to download videos to.</param>
		public static void Main(string playlistId, bool doNotReorder, string downloadFolder)
		{
			if (playlistId == null) throw new ArgumentNullException(nameof(playlistId));
			if (downloadFolder == null) throw new ArgumentNullException(nameof(downloadFolder));

			Directory.CreateDirectory(downloadFolder);

			var httpClient = new HttpClient();
			var seenItems = new SeenItems(downloadFolder);
			var localMetadata = new LocalMetadata(httpClient);
			EnsureFfmpegDependency(new Ffmpeg(httpClient));

			var playlist = GetPlaylist(playlistId);
			EnsureMetadata(playlist, downloadFolder, localMetadata);

			var allVideos = playlist.Videos;
			var sortedVideos = doNotReorder ? allVideos : allVideos.OrderBy(item => item.UploadDate).ToList();
			var newVideos = sortedVideos.Where(video => !seenItems.GetIds().Contains(video.Id));
			ProcessVideos(newVideos, seenItems, downloadFolder, localMetadata);
		}

		private static void EnsureFfmpegDependency(Ffmpeg ffmpeg)
		{
			Console.WriteLine("Finding or downloading ffmpeg");
			ffmpeg.EnsureExists();
		}

		private static Playlist GetPlaylist(string playlistId)
		{
			Console.WriteLine($"Getting playlist {playlistId}");
			var client = new YoutubeClient();
			return client.GetPlaylistAsync(playlistId).Result;
		}

		private static void ProcessVideos(IEnumerable<Video> videos, SeenItems seenItems, string downloadFolder, LocalMetadata localMetadata)
		{
			var client = new YoutubeClient();
			var seasonFolder = Path.Combine(downloadFolder, "Season 1");
			Directory.CreateDirectory(seasonFolder);
			var episodeNumber = GetLastEpisodeNumber(seasonFolder);
			videos.ToList().ForEach(video =>
			{
				Console.WriteLine($"  0.00% Downloading {video.Title}");
				Console.SetCursorPosition(0, Console.CursorTop - 1);

				episodeNumber++;

				var videoFileNameBase = $"S01E{episodeNumber.ToString("N0").PadLeft(2, '0')} " + video.Title.Aggregate("", (agg, cur) => Path.GetInvalidFileNameChars().Contains(cur) ? agg : agg + cur);

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

		private static int GetLastEpisodeNumber(string seasonFolder)
		{
			return Directory
				.EnumerateFiles(seasonFolder, "S01E??*")
				.OrderByDescending(s => s)
				.FirstOrDefault()
				.Convert(
					some: fileName => Regex.Match(fileName, @"S01E(\d\d)").Groups[1].Value.Convert(int.Parse),
					none: () => 0);
		}

		private static void DownloadVideo(IYoutubeClient client, Video video, string downloadFolder, string videoFileNameBase, IProgress<double> progress)
		{
			var converter = new YoutubeConverter(client, Ffmpeg.DefaultFilePath);

			var mediaStreamInfoSet = client.GetVideoMediaStreamInfosAsync(video.Id).Result;
			var videoStreamInfo = mediaStreamInfoSet.Video.OrderByDescending(info => info.VideoQuality).ThenByDescending(info => info.Framerate).First();
			var audioStreamInfo = mediaStreamInfoSet.Audio.OrderByDescending(info => info.Bitrate).First();

			var extension = videoStreamInfo.Container.GetFileExtension();

			converter.DownloadAndProcessMediaStreamsAsync(
					new MediaStreamInfo[] { videoStreamInfo, audioStreamInfo },
					Path.Combine(downloadFolder, videoFileNameBase + $".{extension}"),
					extension,
					progress)
				.Wait();
		}

		private static void DownloadAllCaptions(IYoutubeClient client, Video video, string downloadFolder, string videoFileNameBase, IProgress<double> progress)
		{
			var closedCaptionTrackInfos = client.GetVideoClosedCaptionTrackInfosAsync(video.Id).Result;

			DownloadCaptionsForLanguage(client, closedCaptionTrackInfos, "en", downloadFolder, videoFileNameBase, progress);
			DownloadCaptionsForLanguage(client, closedCaptionTrackInfos, "ko", downloadFolder, videoFileNameBase, progress);
		}

		private static void DownloadCaptionsForLanguage(IYoutubeClient client, IReadOnlyList<ClosedCaptionTrackInfo> closedCaptionTrackInfos, string languageCode, string downloadFolder, string videoFileNameBase, IProgress<double> progress)
		{
			closedCaptionTrackInfos
				.SingleOrDefault(info => !info.IsAutoGenerated && info.Language.Code == languageCode)
				.Do(closedCaptionTrackInfo =>
				{
					Console.WriteLine();
					Console.WriteLine($"  0.00% \t{closedCaptionTrackInfo.Language} captions");
					Console.SetCursorPosition(0, Console.CursorTop - 1);
					client.DownloadClosedCaptionTrackAsync(closedCaptionTrackInfo,
							Path.Combine(downloadFolder, $"{videoFileNameBase}.{languageCode}.srt"),
							progress)
						.Wait();
				});
		}

		private static void EnsureMetadata(Playlist playlist, string downloadFolder, LocalMetadata localMetadata)
		{
			if (!File.Exists(Path.Combine(downloadFolder, "tvshow.nfo")))
			{
				Console.WriteLine("Creating metadata");
				Console.Write("\tInput poster path or URL: ");
				var posterPathOrUrl = Console.ReadLine();
				localMetadata.Save(
					new TVShow(
						title: playlist.Title,
						plot: playlist.Description,
						premiered: playlist.Videos.OrderBy(video => video.UploadDate).First().UploadDate,
						posterPathOrUrl: posterPathOrUrl),
					downloadFolder);
			}
		}

		private static void CreateMetadata(Video video, string downloadFolder, string videoFileNameBase, LocalMetadata localMetadata)
		{
			Console.WriteLine();
			Console.Write("        \tmetadata");
			localMetadata.Save(new Episode(title: video.Title, plot: video.Description, aired: video.UploadDate), downloadFolder, videoFileNameBase);
		}
	}
}
