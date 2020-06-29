using Microsoft.Practices.Unity;
using Quirco.DataPumper;
using System;

namespace TestConsole
{
    class Program
    {
        const string sourceProviderName = "Sql";
        const string sourceConnectionString = "sourceConStr";
        const string targetProviderName = "Sql";
        const string targetConnectionString = "targetConStr";

        private static IUnityContainer _container;

        static void Main(string[] args)
        {
            Init();

            var config = new Configuration();
            var test = config.CurrentDateQuery;
            var logDir = config.LogDir;
            var actual = config.ActualityColumnName;
            var from = config.HistoricColumnFrom;
            var to = config.HistoricColumnTo;
            var jobs = config.Jobs;

            var dataPumperService = _container.Resolve<DataPumperService>();
            dataPumperService.RunJobs(sourceProviderName, sourceConnectionString, targetProviderName, targetConnectionString);
        }

        private static void Init()
        {
            _container = new UnityContainer();
            Bootstrapper.Initialize(_container);
            _container.RegisterType<DataPumperService>();
            _container.RegisterType<IActualityDatesProvider, TestProvider>();
            
        }
    }

    class TestProvider : IActualityDatesProvider
    {
        public DateTime? GetJobActualDate(string jobName)
        {
            throw new NotImplementedException();
        }

        public void SetJobActualDate(string jobName, DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}
