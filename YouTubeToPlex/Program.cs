using System;
using System.CommandLine;
using System.Net.Http;
using YouTubeToPlex.MediaServerHelpers;
using YouTubeToPlex.SubPrograms.Playlist;

namespace YouTubeToPlex
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			var httpClient = new HttpClient();
			var playlistSubprogram = new PlaylistSubProgram(httpClient);

			var rootCommand = new RootCommand("Downloads videos from YouTube and creates metadata for use in media players")
			{
				playlistSubprogram.GetCommand(),
			};

			EnsureFfmpegDependency(new Ffmpeg(httpClient));

			rootCommand.Invoke(args);
		}

		private static void EnsureFfmpegDependency(Ffmpeg ffmpeg)
		{
			Console.WriteLine("Finding or downloading ffmpeg");
			ffmpeg.EnsureExists();
		}
	}
}
