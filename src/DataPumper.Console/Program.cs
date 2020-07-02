using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using DataPumper.Sql;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Quirco.DataPumper;
using Topshelf;

namespace DataPumper.Console
{
    internal class Service
    {
        const string sourceConnectionString = "Server=(local);Database=Logus.HMS.Source;Integrated Security=true;MultipleActiveResultSets=true;Application Name=Logus.Develop.Source";
        const string targetConnectionString = "Server=(local);Database=Logus.HMS.Target;Integrated Security=true;MultipleActiveResultSets=true;Application Name=Logus.Develop.Target";

        private static readonly ILog Log = LogManager.GetLogger(typeof(Service));

        private BackgroundJobServer _jobServer;
        private IDisposable _hangfireDashboard;
        private static IUnityContainer _container;

        private void Init()
        {
            _container = new UnityContainer();
            Bootstrapper.Initialize(_container);
            JobActivator.Current = new UnityJobActivator(_container);
            JobStorage.Current = new MemoryStorage();
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 4,
                DelaysInSeconds = new[] { 1, 5, 60, 300 }
            });
            GlobalConfiguration.Configuration.UseSerializerSettings(new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new PrivateSetterResolver()
            });
            try
            {
                var host = "http://localhost:9019";
                if (!string.IsNullOrEmpty(host))
                {
                    _hangfireDashboard = WebApp.Start<Startup>(host);
                    Log.Warn($"Jobs dashboard is accessible at address: '{host}'");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Can't start Hangfire dashboard", ex);
            }
        }

        public void Start()
        {
            Init();

            _jobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = 5,
                Queues = new[] { "datapumper" }
            });

            var dataPumperService = _container.Resolve<DataPumperService>();

            var sourceProvider = new SqlDataPumperSourceTarget();
            sourceProvider.Initialize(sourceConnectionString);

            var targetProvider = new SqlDataPumperSourceTarget();
            targetProvider.Initialize(targetConnectionString);

            dataPumperService.RunJobs(sourceProvider, targetProvider);

        }

        public void Stop()
        {
            _jobServer?.Dispose();
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
