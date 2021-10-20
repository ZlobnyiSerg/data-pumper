using System;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperTarget : IDataPumperProvider
    {
        Task<long> CleanupTable(CleanupTableRequest request);

        Task<long> InsertData(DataSource dataSource, IDataReader dataReader);

        Task ExecuteRawQuery(string queryText);

        event EventHandler<ProgressEventArgs> Progress;
    }

    public class CleanupTableRequest
    {
        public DataSource DataSource { get; } 

        public string ActualityFieldName { get; } 

        public DateTime? NotOlderThan { get; }
        
        public string InstanceFieldName { get; }

        public string[] InstanceFieldValues { get; }

        public DateTime CurrentPropertyDate { get; }

        public string HistoryDateFromFieldName { get; }

        public bool FullReloading { get; }
        
        public DateTime? DeleteProtectionDate { get; set; }
        
        public FilterConstraint[] Filter { get; set; }

        public CleanupTableRequest(DataSource dataSource, string actualityFieldName, DateTime? notOlderThan, string instanceFieldName, string[] instanceFieldValues, bool fullReloading)
        {
            DataSource = dataSource;
            ActualityFieldName = actualityFieldName;
            NotOlderThan = notOlderThan;
            InstanceFieldName = instanceFieldName;
            InstanceFieldValues = instanceFieldValues;
            FullReloading = fullReloading;
        }

        public CleanupTableRequest(DataSource dataSource, string historyDateFromFieldName, string actualityFieldName, string instanceFieldName, string[] instanceFieldValues, DateTime currentPropertyDate, bool fullReloading)
        {
            DataSource = dataSource;
            HistoryDateFromFieldName = historyDateFromFieldName;
            ActualityFieldName = actualityFieldName;
            CurrentPropertyDate = currentPropertyDate;
            InstanceFieldName = instanceFieldName;
            InstanceFieldValues = instanceFieldValues;
            FullReloading = fullReloading;
        }
    }
}