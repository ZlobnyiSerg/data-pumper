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
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainService));

        private BackgroundJobServer _jobServer;
        private IDisposable _hangfireDashboard;
        private static IUnityContainer _container;
        private static ConsoleConfiguration _configuration = new ConsoleConfiguration();

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
                var host = _configuration.HangFireDashboardUrl;
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

            //var dpConfig = new DataPumperConfiguration();
            //foreach (var job in dpConfig.Jobs)
            //{
            //    BackgroundJob.Enqueue(() => RunJob(job));
            //}

        }

        [JobDisplayName("DataPumper run job {0}")]
        [Queue("datapumper")]
        public async Task RunJob(PumperJobItem jobItem)
        {
            var dataPumperService = new DataPumperService(new Core.DataPumper(), _configuration.TenantCodes);

            var sourceProvider = new SqlDataPumperSourceTarget();
            await sourceProvider.Initialize(_configuration.SourceConnectionString);

            var targetProvider = new SqlDataPumperSourceTarget();
            await targetProvider.Initialize(_configuration.TargetConnectionString);

            await dataPumperService.RunJob(jobItem, sourceProvider, targetProvider);
        }

        [JobDisplayName("DataPumper run all jobs...")]
        [Queue("datapumper")]
        public async Task RunJobs()
        {
            var dataPumperService = new DataPumperService(new Core.DataPumper(), _configuration.TenantCodes);

            var sourceProvider = new SqlDataPumperSourceTarget();
            await sourceProvider.Initialize(_configuration.SourceConnectionString);

            var targetProvider = new SqlDataPumperSourceTarget();
            await targetProvider.Initialize(_configuration.TargetConnectionString);

            await dataPumperService.RunJobs(sourceProvider, targetProvider);
        }

        public void Stop()
        {
            _jobServer?.Dispose();
        }
    }
}
