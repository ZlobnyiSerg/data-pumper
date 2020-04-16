using System;

namespace DataPumper.Core
{
    public class ProgressEventArgs : EventArgs
    {
        public long Processed { get; }
        public string Message { get; }
        public TimeSpan? Elapsed { get; }

        public ProgressEventArgs(long processed, string message, TimeSpan? elapsed = null)
        {
            Processed = processed;
            Message = message;
            Elapsed = elapsed;
        }
    }
}