using System;

namespace DataPumper.Core
{
    public class ProgressEventArgs : EventArgs
    {
        public long Processed { get; }
        public string Message { get; }
        public TimeSpan? Elapsed { get; }
        public DataSource DataSource { get; }

        public ProgressEventArgs(long processed, string message, DataSource dataSource, TimeSpan? elapsed = null)
        {
            Processed = processed;
            Message = message;
            Elapsed = elapsed;
            DataSource = dataSource;
        }
    }
}