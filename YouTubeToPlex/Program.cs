using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using YoutubeExplode;
using YoutubeExplode.Models;

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
	}
}
