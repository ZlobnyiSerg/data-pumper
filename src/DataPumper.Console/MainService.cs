using System;
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
    public class MainService : IDisposable
    {

        public const string Queue = "datapumper";
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

        public void Start()
        {
            Init();

            _jobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = 5,
                Queues = new[] { Queue }
            });

            if (!string.IsNullOrEmpty(_configuration.ScheduleCron))
            {
                RecurringJob.AddOrUpdate(()=>RunJobs(false), _configuration.ScheduleCron);
                RecurringJob.AddOrUpdate(()=>RunJobs(true), Cron.Never);
            }
        }

        [JobDisplayName("DataPumper run job {0}")]
        [Queue(Queue)]
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
        [Queue(Queue)]
        public async Task RunJobs(bool fullReload)
        {
            var dataPumperService = new DataPumperService(new Core.DataPumper(), _configuration.TenantCodes);

            var sourceProvider = new SqlDataPumperSourceTarget();
            await sourceProvider.Initialize(_configuration.SourceConnectionString);            

            var targetProvider = new SqlDataPumperSourceTarget();
            await targetProvider.Initialize(_configuration.TargetConnectionString);

            await dataPumperService.RunJobs(sourceProvider, targetProvider, fullReload);
        }

        public void Stop()
        {
            _jobServer?.Dispose();
        }

        public void Dispose()
        {            
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _jobServer?.Dispose();
                    _hangfireDashboard?.Dispose();
                }
                _disposed = true;
            }
        }

        ~MainService()
        {
            Dispose(false);
        }
    }
}
