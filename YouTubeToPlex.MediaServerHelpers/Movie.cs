using System;

namespace YouTubeToPlex.MediaServerHelpers
{
	public class Movie
	{
		public string Title { get; }
		public string? Plot { get; }
		public DateTimeOffset? ReleaseDate { get; }
		public string? PosterPathOrUrl { get; }

		public Movie(string title, string? plot = null, DateTimeOffset? releaseDate = null, string? posterPathOrUrl = null)
		{
			Title = title;
			Plot = plot;
			ReleaseDate = releaseDate;
			PosterPathOrUrl = posterPathOrUrl;
		}
	}
}
