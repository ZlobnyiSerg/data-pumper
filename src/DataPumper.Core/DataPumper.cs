using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataPumper.Core
{
    public class DataPumper
    {
        private readonly ILogger<DataPumper> _logger;

        public DataPumper(ILogger<DataPumper> logger)
        {
            _logger = logger;
        }

        public async Task<long> Pump(IDataPumperSource source, IDataPumperTarget target,
            TableName sourceTable,
            TableName targetTable,
            string actualityFieldName, TableName instanceTable, 
            DateTime? onDate)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var instances = await source.GetInstances(instanceTable, "PropertyCode");
                _logger.LogInformation($"Cleaning target table '{targetTable}' (after date {onDate}) for instances: {string.Join(',', instances)}...");
                await target.CleanupTable(new CleanupTableRequest(targetTable, actualityFieldName, onDate, "PropertyCode", instances));
                _logger.LogInformation($"Cleaning complete in {sw.Elapsed}, transferring data...");
                sw.Restart();
                using var reader = await source.GetDataReader(sourceTable, actualityFieldName, onDate);
                var items = await target.InsertData(targetTable, reader);
                _logger.LogInformation($"Data transfer of {items} records completed in {sw.Elapsed}");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing {sourceTable} -> {targetTable}", ex);
                throw;
            }
        }
    }
}