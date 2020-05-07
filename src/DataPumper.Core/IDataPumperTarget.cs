using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperTarget : IDataPumperProvider
    {
        Task CleanupTable(CleanupTableRequest request);

        Task<long> InsertData(TableName tableName, IDataReader dataReader);

        event EventHandler<ProgressEventArgs> Progress;
    }

    public class CleanupTableRequest
    {
        public TableName TableName { get; } 
        public string ActualityFieldName { get; } 
        public DateTime? NotOlderThan { get; }
        
        public string InstanceFieldName { get; }
        public string[] InstanceFieldValues { get; }

        public CleanupTableRequest(TableName tableName, string actualityFieldName, DateTime? notOlderThan, string instanceFieldName, string[] instanceFieldValues)
        {
            TableName = tableName;
            ActualityFieldName = actualityFieldName;
            NotOlderThan = notOlderThan;
            InstanceFieldName = instanceFieldName;
            InstanceFieldValues = instanceFieldValues;
        }
    }
}