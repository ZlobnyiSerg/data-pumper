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

namespace DataPumper.Console
{
    public class MainService
    {
        const string sourceConnectionString = "Server=(local);Database=Logus.HMS;Integrated Security=true;MultipleActiveResultSets=true;Application Name=DataPumper";
        const string targetConnectionString = "Server=(local);Database=Logus.HMS.Reporting;Integrated Security=true;MultipleActiveResultSets=true;Application Name=DataPumper";

        private static readonly ILog Log = LogManager.GetLogger(typeof(MainService));

        private BackgroundJobServer _jobServer;
        private IDisposable _hangfireDashboard;
        private static IUnityContainer _container;

        private void Init()
        {
            _container = new UnityContainer();

            Bootstrapper.Initialize(_container);
            Quirco.DataPumper.Bootstrapper.Initialize(_container);

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

        public async void Start()
        {
            Init();

            _jobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = 5,
                Queues = new[] { "datapumper" }
            });

            BackgroundJob.Enqueue(()=> RunJobs());

        }

        [Queue("datapumper")]
        public async Task RunJobs()
        {
            var dataPumperService = _container.Resolve<DataPumperService>();

            var sourceProvider = new SqlDataPumperSourceTarget();
            await sourceProvider.Initialize(sourceConnectionString);

            var targetProvider = new SqlDataPumperSourceTarget();
            await targetProvider.Initialize(targetConnectionString);

            await dataPumperService.RunJobs(sourceProvider, targetProvider);
        }

        public void Stop()
        {
            _jobServer?.Dispose();
        }
    }
}
