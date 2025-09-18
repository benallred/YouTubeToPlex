using System;

namespace YouTubeToPlex
{
    internal class ConcurrentProgress<T> : IProgress<T>
    {
        private readonly object Lock = new object();
        private Action<T> Handler { get; }

        public ConcurrentProgress(Action<T> handler)
        {
            Handler = handler;
        }

        public void Report(T value)
        {
            lock (Lock)
            {
                Handler(value);
            }
        }
    }
}
