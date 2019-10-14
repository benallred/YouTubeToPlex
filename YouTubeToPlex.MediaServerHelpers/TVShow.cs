using System;

namespace YouTubeToPlex.MediaServerHelpers
{
	public class TVShow
	{
		public string Title { get; }
		public string? Plot { get; }
		public DateTimeOffset? Premiered { get; }
		public string? PosterPathOrUrl { get; }

		public TVShow(string title, string? plot = null, DateTimeOffset? premiered = null, string? posterPathOrUrl = null)
		{
			Title = title;
			Plot = plot;
			Premiered = premiered;
			PosterPathOrUrl = posterPathOrUrl;
		}
	}
}
