using System;

namespace DataPumper.Core
{
    public class ProgressEventArgs : EventArgs
    {
        public long Processed { get; }
        public string Message { get; }
        public TimeSpan? Elapsed { get; }
        public TableName TableName { get; }

        public ProgressEventArgs(long processed, string message, TableName tableName, TimeSpan? elapsed = null)
        {
            Processed = processed;
            Message = message;
            Elapsed = elapsed;
            TableName = tableName;
        }
    }
}