using System;
using System.IO;
using System.Net.Http;
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
        public void EnsureExists_DefaultFilePath_FileDoesNotExist()
        {
            Ffmpeg.EnsureExists();
            File.Exists(Ffmpeg.DefaultFilePath).ShouldBeTrue();
        }

        [Fact]
        public void EnsureExists_CustomFilePath_FileDoesNotExist()
        {
            var customFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
            Ffmpeg.EnsureExists(customFilePath);
            File.Exists(customFilePath).ShouldBeTrue();
        }

        [Fact]
        public void EnsureExists_DefaultFilePath_FileExists()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Ffmpeg.DefaultFilePath)!);
            File.WriteAllText(Ffmpeg.DefaultFilePath, "fake file");
            Ffmpeg.EnsureExists();
            File.ReadAllText(Ffmpeg.DefaultFilePath).ShouldBe("fake file");
        }

        [Fact]
        public void EnsureExists_CustomFilePath_FileExists()
        {
            var customFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
            File.WriteAllText(customFilePath, "fake file");
            Ffmpeg.EnsureExists();
            File.ReadAllText(customFilePath).ShouldBe("fake file");
        }
    }
}
