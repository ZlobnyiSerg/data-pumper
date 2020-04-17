using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataPumper.Web.DataLayer
{
    public class TableSyncJob
    {
        public int Id { get; set; }
        
        [Required]
        public string SourceProvider { get; set; }
        [Required]
        public string SourceConnectionString { get; set; }
        [Required]
        public string SourceTableName { get; set; }
        [Required]
        public string TargetProvider { get; set; }
        [Required]
        public string TargetConnectionString { get; set; }
        [Required]
        public string TargetTableName { get; set; }
        public DateTime? Date { get; set; }
        
        public virtual ICollection<SyncJobLog> Log { get; set; }

        public override string ToString()
        {
            return $"Job #{Id}: {SourceTableName} ({SourceProvider}) -> {TargetTableName} ({TargetProvider})";
        }
    }
}