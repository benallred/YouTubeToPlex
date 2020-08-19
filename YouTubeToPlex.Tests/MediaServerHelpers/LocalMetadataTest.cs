using System;
using System.IO;
using System.Net.Http;
using Shouldly;
using Xunit;
using YouTubeToPlex.MediaServerHelpers;

namespace YouTubeToPlex.Tests.MediaServerHelpers
{
	public class LocalMetadataTest : IDisposable
	{
		private FakeHttpMessageHandler FakeHttpMessageHandler { get; }
		private LocalMetadata LocalMetadata { get; }
		private string Folder { get; }

		public LocalMetadataTest()
		{
			FakeHttpMessageHandler = new FakeHttpMessageHandler();
			LocalMetadata = new LocalMetadata(new HttpClient(FakeHttpMessageHandler));
			Folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(Folder);
		}

		public void Dispose()
		{
			Directory.Delete(Folder, true);
		}

		private string GetMetadataFileContents(string fileNameWithoutExtension)
		{
			return File.ReadAllText(Path.Combine(Folder, fileNameWithoutExtension + ".nfo"));
		}

		#region TVShow

		[Fact]
		public void Save_TVShow_Required()
		{
			var metadata = new TVShow("제목");
			LocalMetadata.Save(metadata, Folder);
			GetMetadataFileContents("tvshow").ShouldBe(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<tvshow>
	<title>제목</title>
	<plot />
	<premiered />
</tvshow>"
			);
		}

		[Fact]
		public void Save_TVShow_Optional()
		{
			var metadata = new TVShow(title: "제목", plot: "a\nb", premiered: new DateTime(2019, 02, 03), posterPathOrUrl: "");
			LocalMetadata.Save(metadata, Folder);
			GetMetadataFileContents("tvshow").ShouldBe(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<tvshow>
	<title>제목</title>
	<plot>a
b</plot>
	<premiered>2019-02-03</premiered>
</tvshow>"
			);
		}

		[Fact]
		public void Save_TVShow_PosterPath()
		{
			var fakePosterPath = Path.GetTempFileName();
			var fakePosterContents = "fake poster";
			File.WriteAllText(fakePosterPath, fakePosterContents);

			var metadata = new TVShow(title: "title", posterPathOrUrl: fakePosterPath);
			string posterMetadataPath = Path.Combine(Folder, $"folder{Path.GetExtension(fakePosterPath)}");

			LocalMetadata.Save(metadata, Folder);

			File.Exists(posterMetadataPath).ShouldBeTrue();
			File.ReadAllText(posterMetadataPath).ShouldBe(fakePosterContents);
		}

		[Fact]
		public void Save_TVShow_PosterUrl()
		{
			var fakePosterUrl = "https://fake.com/poster.png";
			var fakePosterContents = "fake poster";
			FakeHttpMessageHandler.Mock(fakePosterUrl, fakePosterContents);

			var metadata = new TVShow(title: "title", posterPathOrUrl: fakePosterUrl);
			string posterMetadataPath = Path.Combine(Folder, "folder.png");

			LocalMetadata.Save(metadata, Folder);

			File.Exists(posterMetadataPath).ShouldBeTrue();
			File.ReadAllText(posterMetadataPath).ShouldBe(fakePosterContents);
		}

		#endregion

		#region Episode

		[Fact]
		public void Save_Episode_Required()
		{
			var metadata = new Episode("제목");
			LocalMetadata.Save(metadata, Folder, metadata.Title);
			GetMetadataFileContents(metadata.Title).ShouldBe(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<episodedetails>
	<title>제목</title>
	<plot />
	<aired />
</episodedetails>"
			);
		}

		[Fact]
		public void Save_Episode_Optional()
		{
			var metadata = new Episode(title: "제목", plot: "a\nb", aired: new DateTime(2019, 02, 03));
			LocalMetadata.Save(metadata, Folder, metadata.Title);
			GetMetadataFileContents(metadata.Title).ShouldBe(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<episodedetails>
	<title>제목</title>
	<plot>a
b</plot>
	<aired>2019-02-03</aired>
</episodedetails>"
			);
		}

		#endregion

		#region Movie

		[Fact]
		public void Save_Movie_Required()
		{
			var metadata = new Movie("제목");
			LocalMetadata.Save(metadata, Folder, metadata.Title);
			GetMetadataFileContents(metadata.Title).ShouldBe(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<movie>
	<title>제목</title>
	<plot />
	<releasedate />
</movie>"
			);
		}

		[Fact]
		public void Save_Movie_Optional()
		{
			var metadata = new Movie(title: "제목", plot: "a\nb", releaseDate: new DateTime(2019, 02, 03), posterPathOrUrl: "");
			LocalMetadata.Save(metadata, Folder, metadata.Title);
			GetMetadataFileContents(metadata.Title).ShouldBe(
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<movie>
	<title>제목</title>
	<plot>a
b</plot>
	<releasedate>2019-02-03</releasedate>
</movie>"
			);
		}

		[Fact]
		public void Save_Movie_PosterPath()
		{
			var fakePosterPath = Path.GetTempFileName();
			var fakePosterContents = "fake poster";
			File.WriteAllText(fakePosterPath, fakePosterContents);

			var metadata = new Movie(title: "title", posterPathOrUrl: fakePosterPath);
			string posterMetadataPath = Path.Combine(Folder, $"{metadata.Title}{Path.GetExtension(fakePosterPath)}");

			LocalMetadata.Save(metadata, Folder, metadata.Title);

			File.Exists(posterMetadataPath).ShouldBeTrue();
			File.ReadAllText(posterMetadataPath).ShouldBe(fakePosterContents);
		}

		[Fact]
		public void Save_Movie_PosterUrl()
		{
			var fakePosterUrl = "https://fake.com/poster.png";
			var fakePosterContents = "fake poster";
			FakeHttpMessageHandler.Mock(fakePosterUrl, fakePosterContents);

			var metadata = new Movie(title: "title", posterPathOrUrl: fakePosterUrl);
			string posterMetadataPath = Path.Combine(Folder, $"{metadata.Title}.png");

			LocalMetadata.Save(metadata, Folder, metadata.Title);

			File.Exists(posterMetadataPath).ShouldBeTrue();
			File.ReadAllText(posterMetadataPath).ShouldBe(fakePosterContents);
		}

		#endregion
	}
}
