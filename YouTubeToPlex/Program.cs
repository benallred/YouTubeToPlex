using System;
using System.CommandLine;
using System.Net.Http;
using System.Threading.Tasks;
using YouTubeToPlex.MediaServerHelpers;
using YouTubeToPlex.SubPrograms.Playlist;
using YouTubeToPlex.SubPrograms.Video;

namespace YouTubeToPlex
{
	internal class Program
	{
		public static async Task<int> Main(string[] args)
		{
			var httpClient = new HttpClient();
			var playlistSubprogram = new PlaylistSubProgram(httpClient);
			var videoSubprogram = new VideoSubProgram(httpClient);

			var rootCommand = new RootCommand("Downloads videos from YouTube and creates metadata for use in media players")
			{
				playlistSubprogram.GetCommand(),
				videoSubprogram.GetCommand(),
			};

			await EnsureFfmpegDependency(new Ffmpeg(httpClient));

			return rootCommand.Invoke(args);
		}

		private static async Task EnsureFfmpegDependency(Ffmpeg ffmpeg)
		{
			Console.WriteLine("Finding or downloading ffmpeg");
			await ffmpeg.EnsureExists();
		}
	}
}
