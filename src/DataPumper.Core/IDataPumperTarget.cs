using System;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperTarget : IDataPumperProvider
    {
        Task CleanupTable(CleanupTableRequest request);

        Task CleanupHistoryTable(CleanupTableRequest request);

        Task<long> InsertData(TableName tableName, IDataReader dataReader);

        Task RunQuery(string queryText);

        event EventHandler<ProgressEventArgs> Progress;
    }

    public class CleanupTableRequest
    {
        public TableName TableName { get; } 

        public string ActualityFieldName { get; } 

        public DateTime? NotOlderThan { get; }
        
        public string InstanceFieldName { get; }

        public string[] InstanceFieldValues { get; }

        public DateTime CurrentPropertyDate { get; }

        public string HistoryDateFromFieldName { get; }

        public bool FullReloading { get; }
        
        public DateTime? DeleteProtectionDate { get; set; }

        public CleanupTableRequest(TableName tableName, string actualityFieldName, DateTime? notOlderThan, string instanceFieldName, string[] instanceFieldValues, bool fullReloading)
        {
            TableName = tableName;
            ActualityFieldName = actualityFieldName;
            NotOlderThan = notOlderThan;
            InstanceFieldName = instanceFieldName;
            InstanceFieldValues = instanceFieldValues;
            FullReloading = fullReloading;
        }

        public CleanupTableRequest(TableName tableName, string historyDateFromFieldName, string instanceFieldName, string[] instanceFieldValues, DateTime currentPropertyDate, bool fullReloading)
        {
            TableName = tableName;
            HistoryDateFromFieldName = historyDateFromFieldName;
            CurrentPropertyDate = currentPropertyDate;
            InstanceFieldName = instanceFieldName;
            InstanceFieldValues = instanceFieldValues;
            FullReloading = fullReloading;
        }
    }
}