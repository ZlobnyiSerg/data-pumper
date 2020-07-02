using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Practices.Unity;
using Quirco.DataPumper;
using Topshelf;

namespace DataPumper.Console
{
    internal class Service
    {
        const string sourceProviderName = "Sql";
        const string sourceConnectionString = "Server=(local);Database=Logus.HMS.Source;Integrated Security=true;MultipleActiveResultSets=true;Application Name=Logus.Develop.Source";
        const string targetProviderName = "Sql";
        const string targetConnectionString = "Server=(local);Database=Logus.HMS.Target;Integrated Security=true;MultipleActiveResultSets=true;Application Name=Logus.Develop.Target";

        private static IUnityContainer _container;

        private void Init()
        {
            _container = new UnityContainer();
            Bootstrapper.Initialize(_container);
            _container.RegisterType<IActualityDatesProvider, TestProvider>();
            _container.RegisterType<DataPumperService>();
        }

        public void Start()
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

        public void Stop()
        {

        }
    }

    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            Log.Info($"Data Pumper is running...");

            HostFactory.Run(x =>
            {
                x.Service<Service>(
                    s =>
                    {
                        s.ConstructUsing(() => new Service());
                        s.WhenStarted(ws => ws.Start());
                        s.WhenStopped(ws => ws.Stop());
                    });
            });

        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
                Log.Fatal("Unhandled exception in service", exception);
            else
                Log.Fatal("Unhandled error of unknown type: " + e.ExceptionObject);
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
