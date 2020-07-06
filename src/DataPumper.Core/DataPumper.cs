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
            TableName instanceTable, 
            DateTime? onDate)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var instances = await source.GetInstances(instanceTable, "PropertyCode");
                _logger.Info($"Cleaning target table '{targetTable}' (after date {onDate}) for instances: {string.Join(",", instances)}...");
                await target.CleanupTable(new CleanupTableRequest(targetTable, actualityFieldName, onDate, "PropertyCode", instances));
                _logger.Info($"Cleaning complete in {sw.Elapsed}, transferring data...");
                sw.Restart();
                using (var reader = await source.GetDataReader(sourceTable, actualityFieldName, onDate))
                {
                    var items = await target.InsertData(targetTable, reader);
                    _logger.Info($"Data transfer of {items} records completed in {sw.Elapsed}");
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