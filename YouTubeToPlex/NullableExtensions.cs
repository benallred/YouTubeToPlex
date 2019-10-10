using System;

namespace YouTubeToPlex
{
	internal static class NullableExtensions
	{
		// I'm not sure if this is completely analogous, but I'm trying to simulate F#'s Option module:
		// https://msdn.microsoft.com/visualfsharpdocs/conceptual/option.iter%5b%27t%5d-function-%5bfsharp%5d
		public static void Iterate<T>(this T nullable, Action<T> action)
			where T : class?
		{
			if (nullable != null)
			{
				action(nullable);
			}
		}
	}
}