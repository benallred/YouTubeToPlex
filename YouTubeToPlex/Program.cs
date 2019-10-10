using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace YouTubeToPlex
{
	internal class Program
	{
		/// <summary>
		/// Downloads videos from a YouTube playlist and creates metadata for use in media players.
		/// </summary>
		/// <param name="playlistId">The ID of the YouTube playlist.</param>
		/// <param name="downloadFolder">The folder to download videos to.</param>
		public static void Main(string playlistId, string downloadFolder)
		{
			if (playlistId == null) throw new ArgumentNullException(nameof(playlistId));
			if (downloadFolder == null) throw new ArgumentNullException(nameof(downloadFolder));

			Directory.CreateDirectory(downloadFolder);
			EnsureFfmpegDependency();

			var seenItems = new SeenItems(downloadFolder);

			var allVideos = GetPlaylistVideos(playlistId);
			var newVideos = FilterAndSortVideos(allVideos, seenItems.GetIds());
			ProcessVideos(newVideos, seenItems, downloadFolder);
		}

		private static void EnsureFfmpegDependency()
		{
			var ffmpegFilePath = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, @"ffmpeg.exe");
			if (!File.Exists(ffmpegFilePath))
			{
				Console.WriteLine("Downloading ffmpeg");
				const string ffmpegZipFileName = "ffmpeg-4.2.1-win64-static.zip";
				var ffmpegZipFilePath = Path.Combine(Path.GetTempPath(), ffmpegZipFileName);
				using var webClient = new WebClient();
				webClient.DownloadFile($"https://ffmpeg.zeranoe.com/builds/win64/static/{ffmpegZipFileName}", ffmpegZipFilePath);
				Console.WriteLine("Extracting ffmpeg");
				ZipFile
					.OpenRead(ffmpegZipFilePath)
					.GetEntry($"{ffmpegZipFileName.Replace(".zip", "")}/bin/ffmpeg.exe")
					.ExtractToFile(ffmpegFilePath);
			}
		}

		private static IReadOnlyList<Video> GetPlaylistVideos(string playlistId)
		{
			Console.WriteLine($"Getting playlist {playlistId}");
			var client = new YoutubeClient();
			var playlist = client.GetPlaylistAsync(playlistId).Result;
			return playlist.Videos;
		}

		private static IEnumerable<Video> FilterAndSortVideos(IEnumerable<Video> allVideos, IEnumerable<string> seenItemIds)
		{
			return allVideos.Where(video => !seenItemIds.Contains(video.Id)).OrderBy(item => item.UploadDate);
		}

		private static void ProcessVideos(IEnumerable<Video> videos, SeenItems seenItems, string downloadFolder)
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

				var videoFileNameBase = $"S01E{episodeNumber.ToString("N0").PadLeft(2, '0')} " + video.Title.Replace('|', '-');

				var progress = new ConcurrentProgress<double>(d =>
				{
					Console.SetCursorPosition(0, Console.CursorTop);
					Console.Write((d * 100).ToString("N2").PadLeft(6));
				});

				DownloadVideo(client, video, seasonFolder, videoFileNameBase, progress);
				DownloadAllCaptions(client, video, seasonFolder, videoFileNameBase, progress);

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
			var converter = new YoutubeConverter(client);

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
	}
}
