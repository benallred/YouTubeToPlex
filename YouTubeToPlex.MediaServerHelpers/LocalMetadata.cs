using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace YouTubeToPlex.MediaServerHelpers
{
	public class LocalMetadata
	{
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
