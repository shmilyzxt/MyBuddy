using System;
using System.Diagnostics;

namespace Belphegor.Utilities
{
    public class PerformanceLogger : IDisposable
    {
        private readonly string _blockName;
        private readonly bool _isEnabled;
        private readonly Stopwatch _stopwatch;
        private bool _isDisposed;

        public PerformanceLogger(bool isEnabled, string blockName)
        {
            _isEnabled = isEnabled;
            _blockName = blockName;
            if (_isEnabled)
            {
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (_isEnabled)
                {
                    _stopwatch.Stop();
                    Logger.WriteVerbose("Execution of the block {0} took {1}ms.", _blockName,
                                        _stopwatch.ElapsedMilliseconds);
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        ~PerformanceLogger()
        {
            Dispose();
        }
    }
}