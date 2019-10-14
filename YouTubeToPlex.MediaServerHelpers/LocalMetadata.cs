using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace YouTubeToPlex.MediaServerHelpers
{
	public class LocalMetadata
	{
		private HttpClient HttpClient { get; }

		public LocalMetadata(HttpClient httpClient)
		{
			HttpClient = httpClient;
		}

		public void Save(TVShow metadata, string folder)
		{
			Save(
				new XDocument(
					new XDeclaration("1.0", "utf-8", "yes"),
					new XElement("tvshow",
						new XElement("title", metadata.Title),
						new XElement("plot", metadata.Plot),
						new XElement("premiered", metadata.Premiered?.ToString("yyyy-MM-dd"))
					)),
				folder, "tvshow");

			if (!string.IsNullOrWhiteSpace(metadata.PosterPathOrUrl))
			{
				var posterMetadataPath = Path.Combine(folder, $"folder{Path.GetExtension(metadata.PosterPathOrUrl)}");
				if (Uri.TryCreate(metadata.PosterPathOrUrl, UriKind.Absolute, out var uri) &&
					!uri.IsFile)
				{
					var response = HttpClient.GetAsync(uri).Result;
					using var fileStream = new FileStream(posterMetadataPath, FileMode.CreateNew);
					response.Content.CopyToAsync(fileStream);
				}
				else
				{
					File.Copy(metadata.PosterPathOrUrl, posterMetadataPath);
				}
			}
		}

		public void Save(Episode metadata, string folder, string fileNameWithoutExtension)
		{
			Save(
				new XDocument(
					new XDeclaration("1.0", "utf-8", "yes"),
					new XElement("episodedetails",
						new XElement("title", metadata.Title),
						new XElement("plot", metadata.Plot),
						new XElement("aired", metadata.Aired?.ToString("yyyy-MM-dd"))
					)),
				folder, fileNameWithoutExtension);
		}

		private static void Save(XDocument xml, string folder, string fileNameWithoutExtension)
		{
			using var xmlWriter = XmlWriter.Create(
				Path.Combine(folder, $"{fileNameWithoutExtension}.nfo"),
				new XmlWriterSettings() { Indent = true, IndentChars = "\t", Encoding = Encoding.UTF8 });
			xml.Save(xmlWriter);
		}
	}
}
