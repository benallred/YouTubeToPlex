using System;
using System.IO;
using System.Text;
using Shouldly;
using Xunit;
using YouTubeToPlex.MediaServerHelpers;

namespace YouTubeToPlex.Tests.MediaServerHelpers
{
	public class LocalMetadataTest : IDisposable
	{
		private string Folder { get; }

		public LocalMetadataTest()
		{
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

		[Fact]
		public void Save_TVShow_Required()
		{
			var metadata = new TVShow("제목");
			new LocalMetadata().Save(metadata, Folder);
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
			var metadata = new TVShow(title: "제목", plot: "a\nb", premiered: new DateTime(2019, 02, 03));
			new LocalMetadata().Save(metadata, Folder);
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
		public void Save_Episode_Required()
		{
			var metadata = new Episode("제목");
			new LocalMetadata().Save(metadata, Folder, metadata.Title);
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
			new LocalMetadata().Save(metadata, Folder, metadata.Title);
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
	}
}
