using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YouTubeToPlex.SubPrograms.Playlist
{
	internal class SeenItems
	{
		private string SeenItemsFilePath { get; }

		public SeenItems(string downloadFolder)
		{
			SeenItemsFilePath = Path.Combine(downloadFolder, $".{nameof(YouTubeToPlex)}.{nameof(SeenItems)}.txt");
		}

		public IEnumerable<string> GetIds()
		{
			return File.Exists(SeenItemsFilePath)
				? File.ReadAllLines(SeenItemsFilePath)
				: Enumerable.Empty<string>();
		}

		public void SaveId(string id)
		{
			File.AppendAllText(SeenItemsFilePath, id + Environment.NewLine);
		}
	}
}
