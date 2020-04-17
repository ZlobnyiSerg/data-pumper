using System;

namespace DataPumper.Web.DataLayer
{
    public class SyncJobLog
    {
        public int Id { get; set; }
        
        public int TableSyncJobId { get; set; }
        
        public virtual TableSyncJob TableSyncJob { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public long RecordsProcessed { get; set; }
        
        public TimeSpan Elapsed { get; set; }

        public string Message { get; set; }
        
        public SyncStatus Status { get; set; }
    }

    public enum SyncStatus
    {
        InProgress = 0,
        Success = 1,
        Error = 2
    }
}