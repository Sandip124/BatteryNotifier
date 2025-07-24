using System;

namespace BatteryNotifier.Lib.CustomTimer
{
    public sealed partial class CustomTimer : IDisposable
    {
        private int _timerCount;
        private bool _disposed;

        public int TimerCount
        {
            get
            {
                ThrowIfDisposed();
                return _timerCount;
            }
            private set
            {
                ThrowIfDisposed();
                _timerCount = value;
            }
        }

        public void ResetTimer()
        {
            ThrowIfDisposed();
            TimerCount = 0;
        }

        public void Increment()
        {
            ThrowIfDisposed();
            TimerCount++;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(CustomTimer));
        }

        // IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources here if any in future
            }

            // Free unmanaged resources here if any in future

            _disposed = true;
        }
    }
}