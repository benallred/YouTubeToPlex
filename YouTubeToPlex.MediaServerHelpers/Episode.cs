using System;

namespace YouTubeToPlex.MediaServerHelpers
{
	public class Episode
	{
		public string Title { get; }
		public string? Plot { get; }
		public DateTimeOffset? Aired { get; }

		public Episode(string title, string? plot = null, DateTimeOffset? aired = null)
		{
			Title = title;
			Plot = plot;
			Aired = aired;
		}
	}
}
