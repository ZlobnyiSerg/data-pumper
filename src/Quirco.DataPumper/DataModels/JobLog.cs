using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quirco.DataPumper.DataModels
{
    [Table("JobLog", Schema ="dp")]
    public class JobLog
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey(nameof(TableSync))]
        public long TableSyncId { get; set; }

        public TableSync TableSync { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime? EndDate { get; set; }

        public long RecordsProcessed { get; set; }

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
