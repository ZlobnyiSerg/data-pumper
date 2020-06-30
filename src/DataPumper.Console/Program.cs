using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Practices.Unity;
using Quirco.DataPumper;
using System;

namespace DataPumper.Console
{
    class Program
    {
        const string sourceProviderName = "Sql";
        const string sourceConnectionString = "sourceConStr";
        const string targetProviderName = "Sql";
        const string targetConnectionString = "targetConStr";

        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private static IUnityContainer _container;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

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

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
                Log.Fatal("Unhandled exception in service", exception);
            else
                Log.Fatal("Unhandled error of unknown type: " + e.ExceptionObject);
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
