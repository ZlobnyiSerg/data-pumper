using Common.Logging;
using DataPumper.Core;
using DataPumper.Sql;
using Hangfire;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDataPumper = DataPumper.Core;

namespace Quirco.DataPumper
{
    public class DataPumperService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperService));

        private readonly IActualityDatesProvider _actualityDatesProvider;
        private readonly NDataPumper.DataPumper _pumper;
        private readonly Configuration _configuration;
        private readonly IUnityContainer _container;

        public DataPumperService(IActualityDatesProvider actualityDatesProvider,
            NDataPumper.DataPumper dataPumper, 
            IUnityContainer container)
        {
            _actualityDatesProvider = actualityDatesProvider;
            _pumper = dataPumper;
            _configuration = new Configuration();
            _container = container;
        }

        public void RunJobs(string sourceProviderName, string sourceConnectionString, string targetProviderName, string targetConnectionString)
        {
            Log.Info("Performing synchronization for all jobs...");
            var configuration = new Configuration();
            var jobs = configuration.Jobs;

            BackgroundJob.Enqueue(() => ProcessInternal(jobs, sourceProviderName, sourceConnectionString, targetProviderName, targetConnectionString));
        }

        [Queue("datapumper")]
        public async Task ProcessInternal(ConfigJobItem[] jobs, string sourceProviderName, string sourceConnectionString, string targetProviderName, string targetConnectionString)
        {
            Log.Warn("Started job to sync all tables...");

            var sourceProvider = await GetProvider<IDataPumperSource>(sourceProviderName, sourceConnectionString);
            var targetProvider = await GetProvider<IDataPumperTarget>(targetProviderName, targetConnectionString);

            var tasks = jobs.ToList().Select(j => RunJobInternal(j, sourceProvider, targetProvider));
            var results = await Task.WhenAll(tasks);

            ResultWriteToFile(results);            
        }

        private async Task<JobLog> RunJobInternal(ConfigJobItem job, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Warn($"Processing {job.Name}");
            var log = new JobLog { Name = job.Name};

            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var jobActualDate = _actualityDatesProvider.GetJobActualDate(job.Name);
                var onDate = jobActualDate == null ? DateTime.Today.AddYears(-100) : jobActualDate;

                var currentDate = await sourceProvider.GetCurrentDate(_configuration.CurrentDateQuery) ?? DateTime.Now.Date;

                var records = await _pumper.Pump(sourceProvider, targetProvider,
                    new TableName(job.SourceTableName),
                    new TableName(job.TargetTableName), 
                    _configuration.ActualityColumnName,
                    new TableName("lr.VProperties"),
                    onDate,
                    job.HistoricMode,
                    currentDate);

                _actualityDatesProvider.SetJobActualDate(job.Name, currentDate);

                log.ElapsedTime = sw.Elapsed;
                log.RecordsProcessed = records;
                log.ActualDate = currentDate;
                sw.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing job {job}", ex);
                log.Error = ex.Message;
            }

            return log;
        }

        private async Task<T> GetProvider<T>(string providerName, string connectionString) where T : IDataPumperProvider
        {
            var providers = _container.ResolveAll<T>();
            if (providers == null || !providers.Any())
                throw new ApplicationException($"{typeof(T).Name} types not registered");

            foreach (var provider in providers)
            {
                if (providerName == provider.GetName())
                {
                    await provider.Initialize(connectionString);
                    return provider;
                }
            }

            throw new ApplicationException($"No target provider named '{providerName}'");
        }

        private void ResultWriteToFile(JobLog[] results)
        {
            Log.Trace(JsonConvert.SerializeObject(results, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
            var dir = Path.Combine(Environment.CurrentDirectory, _configuration.LogDir);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (StreamWriter file = File.CreateText(Path.Combine(dir, $"{DateTime.Now:yyyyMMddTHHmmss}.log")))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };
                serializer.Serialize(file, results);
            }
        }
    }
}
