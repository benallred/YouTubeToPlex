using System;

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
		}
	}
}
