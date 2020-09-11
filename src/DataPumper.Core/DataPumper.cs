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

        public async Task<long> Pump(IDataPumperSource source, IDataPumperTarget target,
            TableName sourceTable,
            TableName targetTable,
            string actualityFieldName, 
            string historyDateFromFieldName,
            string tenantField, 
            DateTime onDate,
            bool historicMode, 
            DateTime currentDate,
            bool fullReloading, 
            string[] tenantCodes)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                if (historicMode)
                {
                    _logger.Info($"Cleaning target table in history mode '{targetTable}' (historyDateFrom {currentDate}) for instances: {string.Join(",", tenantCodes ?? new string[0])}...");
                    await target.CleanupHistoryTable(new CleanupTableRequest(targetTable, historyDateFromFieldName, tenantField, tenantCodes, currentDate, fullReloading));
                    _logger.Info($"Cleaning '{targetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }
                else
                {
                    _logger.Info($"Cleaning target table '{targetTable}' (from date {onDate}) for instances: {string.Join(",", tenantCodes ?? new string[0])}...");
                    await target.CleanupTable(new CleanupTableRequest(targetTable, actualityFieldName, onDate, tenantField, tenantCodes, fullReloading));
                    _logger.Info($"Cleaning '{targetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }

                using (var reader = await source.GetDataReader(sourceTable, actualityFieldName, onDate, tenantField, tenantCodes))
                {
                    var items = await target.InsertData(targetTable, reader);
                    _logger.Info($"Data transfer '{targetTable}' of {items} records completed in {sw.Elapsed}");

                    return items;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing {sourceTable} -> {targetTable}", ex);
                throw;
            }
        }
    }
}