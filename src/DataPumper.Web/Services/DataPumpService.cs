using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataPumper.Core;
using Microsoft.Extensions.Logging;

namespace DataPumper.Web.Services
{
    public class DataPumpService
    {
        private readonly Core.DataPumper _pumper;
        private readonly ILogger<DataPumpService> _logger;
        public IEnumerable<IDataPumperSource> Sources { get; }
        public IEnumerable<IDataPumperTarget> Targets { get; }

        public DataPumpService(IEnumerable<IDataPumperSource> sources, 
            IEnumerable<IDataPumperTarget> targets, 
            DataPumper.Core.DataPumper pumper,
            ILogger<DataPumpService> logger)
        {
            _pumper = pumper;
            _logger = logger;
            Sources = sources;
            Targets = targets;
        }

        public EventHandler<ProgressEventArgs> Progress;

        public async Task<long> Process()
        {
            var source = Sources.First();
            var target = Targets.First();
            await source.Initialize("Server=(local);Database=Logus.HMS;Integrated Security=true;MultipleActiveResultSets=true;Application Name=Logus");
            await target.Initialize("Server=(local);Database=Logus.Reporting;Integrated Security=true;MultipleActiveResultSets=true;Application Name=Logus");
            
            target.Progress += TargetOnProgress;
            var records = await _pumper.Pump(source, target, new TableName("lr", "VTransactions"), new TableName("lr", "Transactions"), "ActualDate", null);
            return records;
        }

        private void TargetOnProgress(object? sender, ProgressEventArgs args)
        {
            var handler = Progress;
            handler?.Invoke(sender, args);
        }
    }
}