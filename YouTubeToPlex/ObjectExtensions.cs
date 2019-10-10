using System;

namespace YouTubeToPlex
{
	internal static class ObjectExtensions
	{
		public static TTo Convert<TFrom, TTo>(this TFrom obj, Func<TFrom, TTo> convert)
		{
			return convert(obj);
		}

		public static TTo Convert<TFrom, TTo>(this TFrom? obj, Func<TFrom, TTo> some, Func<TTo> none)
			where TFrom : class
		{
			return obj != null
				? some(obj)
				: none();
		}

		public static void Do<T>(this T? obj, Action<T> action, Action? none = null)
			where T : class
		{
			if (obj != null)
			{
				action(obj);
			}
			else
			{
				none?.Invoke();
			}
		}
	}
}
