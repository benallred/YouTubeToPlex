using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;

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
	}
}
