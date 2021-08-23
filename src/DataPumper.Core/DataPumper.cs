using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common.Logging;

namespace DataPumper.Core
{
    public class DataPumper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumper));

        public async Task<long> Pump(IDataPumperSource source, IDataPumperTarget target, PumpParameters parameters)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                if (parameters.HistoricMode)
                {
                    Log.Info(
                        $"Cleaning target table in history mode '{parameters.TargetTable}' (historyDateFrom {parameters.CurrentDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                    await target.CleanupTable(new CleanupTableRequest(
                        parameters.TargetTable,
                        parameters.HistoryDateFromFieldName,
                        parameters.ActualityFieldName,
                        parameters.TenantField,
                        parameters.TenantCodes,
                        parameters.CurrentDate,
                        parameters.FullReloading)
                    {
                        DeleteProtectionDate = parameters.DeleteProtectionDate,
                        Filter = parameters.Filter,
                    });
                    Log.Info($"Cleaning '{parameters.TargetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }
                else
                {
                    Log.Info(
                        $"Cleaning target table '{parameters.TargetTable}' (from date {parameters.OnDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                    await target.CleanupTable(new CleanupTableRequest(
                        parameters.TargetTable,
                        parameters.ActualityFieldName,
                        parameters.OnDate,
                        parameters.TenantField,
                        parameters.TenantCodes,
                        parameters.FullReloading)
                    {
                        DeleteProtectionDate = parameters.DeleteProtectionDate,
                        Filter = parameters.Filter,
                    });
                    Log.Info($"Cleaning '{parameters.TargetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }

                using (var reader = await source.GetDataReader(
                    new DataReaderRequest(parameters.SourceTable, parameters.ActualityFieldName)
                    {
                        NotOlderThan = parameters.OnDate,
                        TenantField = parameters.TenantField,
                        TenantCodes = parameters.TenantCodes,
                        Filter = parameters.Filter
                    }))
                {
                    var items = await target.InsertData(parameters.TargetTable, reader);
                    Log.Info($"Data transfer '{parameters.TargetTable}' of {items} records completed in {sw.Elapsed}");

                    return items;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing {parameters.SourceTable} -> {parameters.TargetTable}", ex);
                throw;
            }
        }
    }
}