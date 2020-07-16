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
            TableName instanceTable, 
            DateTime onDate,
            bool historicMode, 
            DateTime currentDate)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var instances = await source.GetInstances(instanceTable, "PropertyCode");

                if (historicMode)
                {
                    _logger.Info($"Cleaning target table in history mode '{targetTable}' (history date from {currentDate}) for instances: {string.Join(",", instances)}...");
                    await target.CleanupHistoryTable(new CleanupTableRequest(targetTable, historyDateFromFieldName, "PropertyCode", instances, currentDate));
                    _logger.Info($"Cleaning '{targetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }
                else
                {
                    _logger.Info($"Cleaning target table '{targetTable}' (after date {onDate}) for instances: {string.Join(",", instances)}...");
                    await target.CleanupTable(new CleanupTableRequest(targetTable, actualityFieldName, onDate, "PropertyCode", instances));
                    _logger.Info($"Cleaning '{targetTable}' complete in {sw.Elapsed}, transferring data...");
                    sw.Restart();
                }

                using (var reader = await source.GetDataReader(sourceTable, actualityFieldName, onDate))
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