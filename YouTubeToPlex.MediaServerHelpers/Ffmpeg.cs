using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace YouTubeToPlex.MediaServerHelpers
{
	public class Ffmpeg
	{
		public static string DefaultFilePath = Path.Combine(Path.GetTempPath(), nameof(MediaServerHelpers), "ffmpeg.exe");

		private HttpClient HttpClient { get; }

		public Ffmpeg(HttpClient httpClient)
		{
			HttpClient = httpClient;
		}

		public void EnsureExists(string? customFilePath = null)
		{
			var ffmpegFilePath = customFilePath ?? DefaultFilePath;
			// var ffmpegFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, @"ffmpeg.exe");
			if (!File.Exists(ffmpegFilePath))
			{
				const string ffmpegZipFileName = "ffmpeg-release-essentials.zip";
				var ffmpegZipFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), ffmpegZipFileName);
				DownloadZip($"https://www.gyan.dev/ffmpeg/builds/{ffmpegZipFileName}", ffmpegZipFilePath);
				ExtractFile(ffmpegZipFilePath, ffmpegFilePath);
			}
		}

		private void DownloadZip(string uri, string downloadToPath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(downloadToPath)!);
			var response = HttpClient.GetAsync(uri).Result.Convert(
				httpResponseMessage => httpResponseMessage.Headers.Location.Case(
					some: redirectUri => HttpClient.GetAsync(redirectUri).Result,
					none: () => httpResponseMessage));
			using var fileStream = new FileStream(downloadToPath, FileMode.CreateNew);
			response.Content.CopyToAsync(fileStream).Wait();
		}

		private static void ExtractFile(string zipFilePath, string extractToPath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(extractToPath)!);
			ZipFile
				.OpenRead(zipFilePath)
				.Entries
				.Single(entry => entry.Name == "ffmpeg.exe")
				.ExtractToFile(extractToPath);
		}
	}
}
