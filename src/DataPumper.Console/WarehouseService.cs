using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using DataPumper.Core;
using DataPumper.Sql;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Quirco.DataPumper;
using Quirco.DataPumper.DataModels;

namespace DataPumper.Console
{
    public class WarehouseService : IDisposable
    {
        public const string Queue = "datapumper";
        private static readonly ILog Log = LogManager.GetLogger(typeof(WarehouseService));

        protected BackgroundJobServer JobServer { get; private set; }
        private IDisposable _hangfireDashboard;
        private static IUnityContainer _container;
        private WarehouseServiceConfiguration _configuration;
        private IConfigurationRoot _configSource;

        public WarehouseService()
        {
            _configSource = new ConfigurationBuilder()
                .AddXmlFile("data-pumper.config")
                .AddXmlFile("data-pumper.local.config", true)
                .Build();
            _configuration = new WarehouseServiceConfiguration(_configSource);
            ConfigurationManager.Configuration = _configSource;

            using var ctx = new DataPumperContext(_configuration.MetadataConnectionString);
            ctx.TableSyncs.ToList();
        }

        private void Init()
        {
            _container = new UnityContainer();

            Bootstrapper.Initialize(_container, _configuration);

            JobActivator.Current = new UnityJobActivator(_container);
            JobStorage.Current = new SqlServerStorage(_configuration.HangfireConnectionString, new SqlServerStorageOptions
            {
                InvisibilityTimeout = TimeSpan.FromHours(3)
            });
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 3,
                DelaysInSeconds = new[] {1, 10, 60}
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

            JobServer = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = 1,
                Queues = new[] {Queue}
            });

            RecurringJob.AddOrUpdate(() => RunPartialUpdate(), Cron.Never);
            
            if (!string.IsNullOrEmpty(_configuration.ScheduleCron))
            {
                RecurringJob.AddOrUpdate(() => RunJobs(), _configuration.ScheduleCron);
                RecurringJob.AddOrUpdate(() => RunJobsWithReload(), Cron.Never);
                
                var dataPumperConfig = new DataPumperConfiguration(_configSource);
                foreach (var job in dataPumperConfig.Jobs)
                {
                    RecurringJob.AddOrUpdate(job.Name, ()=> RunJob(job, false), Cron.Never);
                    RecurringJob.AddOrUpdate(job.Name+"-full", ()=> RunJob(job, true), Cron.Never);
                }
            }
        }

        [JobDisplayName("Run single job: {0}")]
        [Queue(Queue)]
        public async Task RunJob(PumperJobItem jobItem, bool fullReload)
        {
            var dataPumperService = new DataPumperService(new DataPumperConfiguration(_configSource), _configuration.TenantCodes);

            var sourceProvider = _container.Resolve<IDataPumperSource>(_configuration.SourceProvider);
            await sourceProvider.Initialize(_configuration.SourceConnectionString);

            var targetProvider = _container.Resolve<IDataPumperTarget>(_configuration.TargetProvider);
            await targetProvider.Initialize(_configuration.TargetConnectionString);

            await dataPumperService.RunJob(jobItem, sourceProvider, targetProvider, fullReload);
        }

        [JobDisplayName("Run all jobs")]
        [Queue(Queue)]
        public Task RunJobs()
        {
            return RunJobs(false);
        }
        
        [JobDisplayName("Run all jobs with reload")]
        [Queue(Queue)]
        public Task RunJobsWithReload()
        {
            return RunJobs(true);
        }
        
        [JobDisplayName("Run partial update")]
        [Queue(Queue)]
        public async Task RunPartialUpdate()
        {
            var sourceProvider = _container.Resolve<IDataPumperSource>(_configuration.SourceProvider);
            await sourceProvider.Initialize(_configuration.SourceConnectionString);

            var targetProvider = _container.Resolve<IDataPumperTarget>(_configuration.TargetProvider);
            await targetProvider.Initialize(_configuration.TargetConnectionString);
        }
        
        
        private async Task RunJobs(bool fullReload)
        {
            var dataPumperService = new DataPumperService(new DataPumperConfiguration(_configSource), _configuration.TenantCodes);

            var sourceProvider = _container.Resolve<IDataPumperSource>(_configuration.SourceProvider);
            await sourceProvider.Initialize(_configuration.SourceConnectionString);

            var targetProvider = _container.Resolve<IDataPumperTarget>(_configuration.TargetProvider);
            await targetProvider.Initialize(_configuration.TargetConnectionString);

            await dataPumperService.RunJobs(sourceProvider, targetProvider, fullReload);
        }

        public void Stop()
        {
            JobServer?.Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
            }
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                JobServer?.Dispose();
                _hangfireDashboard?.Dispose();

                _disposed = true;

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        ~WarehouseService()
        {
            Dispose(false);
        }
    }
}