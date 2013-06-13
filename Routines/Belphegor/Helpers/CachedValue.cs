using System;
using System.Diagnostics;

namespace Belphegor.Helpers
{
    /// <summary> Cached value. </summary>
    /// <remarks> Nesox, 2013-01-14. </remarks>
    /// <typeparam name="T"> Generic type parameter. </typeparam>
    public class CachedValue<T> where T : struct 
    {
        private T? _cachedValue;
        private readonly TimeSpan _duration;
        private readonly Func<T> _retriever;

        /// <summary> Constructor. </summary>
        /// <remarks> Nesox, 2013-01-14. </remarks>
        /// <param name="retriever"> The retriever. </param>
        /// <param name="duration">  The duration. </param>
        public CachedValue(Func<T> retriever, TimeSpan duration)
        {
            _retriever = retriever;
            _cachedValue = retriever();
            _duration = duration;
        }

        private Stopwatch _timer;
        /// <summary> Gets the value. </summary>
        /// <value> The value. </value>
        public T Value
        {
            get
            {
                if (!_cachedValue.HasValue)
                    _cachedValue = _retriever();

                if (_timer == null)
                    _timer = Stopwatch.StartNew();

                if (_timer.Elapsed > (_timer.Elapsed - _duration))
                    return _cachedValue.Value;

                _cachedValue = _retriever();
                _timer = null;

                return _cachedValue.Value;
            }
        }

        /// <summary>
        /// Gets the real, non-cached value. Calling this does not affect the cache itself, and does not reset any of its timers.
        /// </summary>
        public T RealValue
        {
            get { return _retriever(); }
        }
    }
}
