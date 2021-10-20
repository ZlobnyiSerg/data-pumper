using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common.Logging;

namespace DataPumper.Core
{
    public class DataPumper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumper));

        public async Task<PumpResult> Pump(IDataPumperSource source, IDataPumperTarget target, PumpParameters parameters)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var deleted = 0L;

                if (parameters.HistoricMode)
                {
                    Log.Info(
                        $"Cleaning target table in history mode '{parameters.TargetDataSource}' (historyDateFrom {parameters.CurrentDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                    deleted =await target.CleanupTable(new CleanupTableRequest(
                        parameters.TargetDataSource,
                        parameters.HistoryDateFromFieldName,
                        parameters.ActualityFieldName,
                        parameters.TenantField,
                        parameters.TenantCodes,
                        parameters.CurrentDate,
                        parameters.FullReloading)
                    {
                        DeleteProtectionDate = parameters.DeleteProtectionDate,
                        Filter = parameters.Filter
                    });
                    Log.Info($"Cleaning '{parameters.TargetDataSource}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }
                else
                {
                    Log.Info(
                        $"Cleaning target table '{parameters.TargetDataSource}' (from date {parameters.OnDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                    deleted = await target.CleanupTable(new CleanupTableRequest(
                        parameters.TargetDataSource,
                        parameters.ActualityFieldName,
                        parameters.OnDate,
                        parameters.TenantField,
                        parameters.TenantCodes,
                        parameters.FullReloading)
                    {
                        DeleteProtectionDate = parameters.DeleteProtectionDate,
                        Filter = parameters.Filter
                    });
                    Log.Info($"Cleaning '{parameters.TargetDataSource}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }

                using var reader = await source.GetDataReader(
                    new DataReaderRequest(parameters.SourceDataSource, parameters.ActualityFieldName)
                    {
                        NotOlderThan = parameters.OnDate,
                        TenantField = parameters.TenantField,
                        TenantCodes = parameters.TenantCodes,
                        Filter = parameters.Filter
                    });
                var inserted = await target.InsertData(parameters.TargetDataSource, reader);
                Log.Info($"Data transfer '{parameters.TargetDataSource}' of {inserted} records completed in {sw.Elapsed}");

                return new PumpResult(inserted, deleted);
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing {parameters.SourceDataSource} -> {parameters.TargetDataSource}", ex);
                throw;
            }
        }
    }

    public readonly struct PumpResult
    {
        public long Inserted { get; }
        
        public long Deleted { get; }

        public PumpResult(long inserted, long deleted)
        {
            Inserted = inserted;
            Deleted = deleted;
        }
    }
}