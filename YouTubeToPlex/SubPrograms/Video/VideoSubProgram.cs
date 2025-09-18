using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using YoutubeExplode;
using YouTubeToPlex.MediaServerHelpers;

using YTVideo = YoutubeExplode.Videos.Video;

namespace YouTubeToPlex.SubPrograms.Video
{
    internal class VideoSubProgram : ISubProgram
    {
        private HttpClient HttpClient { get; }

        public VideoSubProgram(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public Command GetCommand()
        {
            var command = new Command("video", "Downloads a single YouTube video")
            {
                new Option<string>("--id", "The ID of the YouTube video"),
                new Option<string>("--download-folder", "The folder to download the video to"),
            };
            command.Handler = CommandHandler.Create<string, string>(DownloadVideo);
            return command;
        }

        public void DownloadVideo(string id, string downloadFolder)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (downloadFolder == null) throw new ArgumentNullException(nameof(downloadFolder));

            Directory.CreateDirectory(downloadFolder);

            var localMetadata = new LocalMetadata(HttpClient);

            var video = GetVideo(id);
            var videoFileNameBase = video.Title.Aggregate("", (agg, cur) => Path.GetInvalidFileNameChars().Contains(cur) ? agg : agg + cur);
            SaveMetadata(video, downloadFolder, videoFileNameBase, localMetadata);
            SaveVideo(video, downloadFolder, videoFileNameBase);
        }

        private YTVideo GetVideo(string videoId)
        {
            Console.WriteLine($"Getting video {videoId}");
            var client = new YoutubeClient();
            return client.Videos.GetAsync(videoId).Result;
        }

        private void SaveVideo(YTVideo video, string downloadFolder, string videoFileNameBase)
        {
            var client = new YoutubeClient();

            Console.WriteLine($"  0.00% Downloading {video.Id} {video.Title}");
            Console.SetCursorPosition(0, Console.CursorTop - 1);

            var progress = new ConcurrentProgress<double>(d =>
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write((d * 100).ToString("N2").PadLeft(6));
            });

            Downloaders.DownloadVideo(client, video, downloadFolder, videoFileNameBase, progress);
            Downloaders.DownloadAllCaptions(client, video, downloadFolder, videoFileNameBase, progress);

            Console.WriteLine();
        }

        private void SaveMetadata(YTVideo video, string downloadFolder, string videoFileNameBase, LocalMetadata localMetadata)
        {
            Console.WriteLine("Creating metadata");
            Console.Write("\tInput poster path or URL: ");
            var posterPathOrUrl = Console.ReadLine();
            localMetadata.Save(
                new Movie(
                    title: video.Title,
                    plot: video.Description,
                    releaseDate: video.UploadDate,
                    posterPathOrUrl: posterPathOrUrl),
                downloadFolder,
                videoFileNameBase);
        }
    }
}
