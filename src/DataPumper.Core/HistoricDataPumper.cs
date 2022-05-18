using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using Common.Logging;

namespace DataPumper.Core
{
    public class HistoricDataPumper : IDataPumper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumper));
        
        public async Task<PumpResult> Pump(IDataPumperSource source, IDataPumperTarget target, PumpParameters parameters)
        {
            try 
            {
                var sw = new Stopwatch();
                sw.Start();

                Log.Info(
                    $"Cleaning target table '{parameters.TargetDataSource}' (from date {parameters.OnDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                var cleanupTableRequest = new CleanupTableRequest(
                    parameters.TargetDataSource,
                    parameters.ActualityFieldName,
                    parameters.OnDate,
                    parameters.CurrentDate,
                    parameters.TenantField,
                    parameters.TenantCodes
                )
                {
                    DeleteProtectionDate = parameters.DeleteProtectionDate,
                    Filter = parameters.Filter
                };
                var deleted = await target.CleanupHistoryTable(cleanupTableRequest);
            
                Log.Info($"Cleaning '{parameters.TargetDataSource}' completed in {sw.Elapsed}, records deleted: {deleted}, transferring data...");
                sw.Restart();

                using var reader = await source.GetDataReader(
                    new DataReaderRequest(parameters.SourceDataSource, parameters.ActualityFieldName)
                    {
                        NotOlderThan = parameters.OnDate,
                        TenantField = parameters.TenantField,
                        TenantCodes = parameters.TenantCodes,
                        Filter = parameters.Filter
                    });
                var inserted = await target.InsertData(parameters.TargetDataSource, reader);
                Log.Info($"Data transfer '{parameters.TargetDataSource}' of {inserted} record(s) completed in {sw.Elapsed}");

                var updated = await target.CloseHistoricPeriods(cleanupTableRequest);
                Log.Info($"Updated {updated} historic record(s)");

                return new PumpResult(inserted, deleted);
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing {parameters.SourceDataSource} -> {parameters.TargetDataSource}", ex);
                throw;
            }
        }
    }
}