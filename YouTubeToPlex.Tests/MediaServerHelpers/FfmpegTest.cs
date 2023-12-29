using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using YouTubeToPlex.MediaServerHelpers;

namespace YouTubeToPlex.Tests.MediaServerHelpers
{
	public class FfmpegTest : IDisposable
	{
		private Ffmpeg Ffmpeg { get; }

		public FfmpegTest()
		{
			Ffmpeg = new Ffmpeg(new HttpClient());

			CleanDefaultFilePath();
		}

		public void Dispose()
		{
			CleanDefaultFilePath();
		}

		private static void CleanDefaultFilePath()
		{
			var defaultFfmpegDirectory = Path.GetDirectoryName(Ffmpeg.DefaultFilePath);
			if (Directory.Exists(defaultFfmpegDirectory))
			{
				Directory.Delete(defaultFfmpegDirectory, true);
			}
		}

		[Fact]
		public void DefaultFilePath()
		{
			Ffmpeg.DefaultFilePath.ShouldBe(Path.Combine(Path.GetTempPath(), nameof(MediaServerHelpers), "ffmpeg.exe"));
		}

		[Fact]
		public async Task EnsureExists_DefaultFilePath_FileDoesNotExistAsync()
		{
			await Ffmpeg.EnsureExists();
			File.Exists(Ffmpeg.DefaultFilePath).ShouldBeTrue();
		}

		[Fact]
		public async Task EnsureExists_CustomFilePath_FileDoesNotExistAsync()
		{
			var customFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
			await Ffmpeg.EnsureExists(customFilePath);
			File.Exists(customFilePath).ShouldBeTrue();
		}

		[Fact]
		public async Task EnsureExists_DefaultFilePath_FileExistsAsync()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Ffmpeg.DefaultFilePath)!);
			File.WriteAllText(Ffmpeg.DefaultFilePath, "fake file");
			await Ffmpeg.EnsureExists();
			File.ReadAllText(Ffmpeg.DefaultFilePath).ShouldBe("fake file");
		}

		[Fact]
		public async Task EnsureExists_CustomFilePath_FileExistsAsync()
		{
			var customFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
			File.WriteAllText(customFilePath, "fake file");
			await Ffmpeg.EnsureExists();
			File.ReadAllText(customFilePath).ShouldBe("fake file");
		}
	}
}
