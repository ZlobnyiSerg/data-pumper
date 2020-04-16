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

        public async Task<long> Pump(IDataPumperSource source, IDataPumperTarget target, TableName sourceTable, TableName targetTable, string fieldName, DateTime? onDate)
        {
            var sw = new Stopwatch();
            sw.Start();
            _logger.LogInformation($"Cleaning target table '{targetTable}' (after date {onDate})...");
            await target.CleanupTable(targetTable, fieldName, onDate);
            _logger.LogInformation($"Cleaning complete in {sw.Elapsed}, transferring data...");
            sw.Restart();
            using var reader = await source.GetDataReader(sourceTable, fieldName, onDate);
            var items = await target.InsertData(targetTable, reader);
            _logger.LogInformation($"Data transfer of {items} records completed in {sw.Elapsed}");
            return items;
        }
    }
}