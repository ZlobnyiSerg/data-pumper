using Common.Logging;
using DataPumper.Core;
using DataPumper.Sql;
using Microsoft.Practices.Unity;
using System;

namespace Quirco.DataPumper
{
    public class DataPumperService
    {
        private readonly IActualityDatesProvider _actualityDatesProvider;

        public DataPumperService(IActualityDatesProvider actualityDatesProvider)
        {
            _actualityDatesProvider = actualityDatesProvider;
        }

        public async void RunJobs(IDataPumperProvider sourceProvider, IDataPumperProvider targetProvider)
        {
            
        }
    }
}
