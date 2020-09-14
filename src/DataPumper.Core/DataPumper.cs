using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common.Logging;

namespace DataPumper.Core
{
    public class DataPumper
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DataPumper));

        public DataPumper()
        {
        }

        public async Task<long> Pump(IDataPumperSource source, IDataPumperTarget target, PumpParameters parameters)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                if (parameters.HistoricMode)
                {
                    _logger.Info($"Cleaning target table in history mode '{parameters.TargetTable}' (historyDateFrom {parameters.CurrentDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                    await target.CleanupHistoryTable(new CleanupTableRequest(parameters.TargetTable, parameters.HistoryDateFromFieldName, parameters.TenantField, parameters.TenantCodes, parameters.CurrentDate, parameters.FullReloading));
                    _logger.Info($"Cleaning '{parameters.TargetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }
                else
                {
                    _logger.Info($"Cleaning target table '{parameters.TargetTable}' (from date {parameters.OnDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                    await target.CleanupTable(new CleanupTableRequest(parameters.TargetTable, parameters.ActualityFieldName, parameters.OnDate, parameters.TenantField, parameters.TenantCodes, parameters.FullReloading));
                    _logger.Info($"Cleaning '{parameters.TargetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }

                using (var reader = await source.GetDataReader(parameters.SourceTable, parameters.ActualityFieldName, parameters.OnDate, parameters.TenantField, parameters.TenantCodes))
                {
                    var items = await target.InsertData(parameters.TargetTable, reader);
                    _logger.Info($"Data transfer '{parameters.TargetTable}' of {items} records completed in {sw.Elapsed}");

                    return items;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing {parameters.SourceTable} -> {parameters.TargetTable}", ex);
                throw;
            }
        }
    }
}