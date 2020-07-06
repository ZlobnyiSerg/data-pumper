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

        public DataPumperService(IActualityDatesProvider actualityDatesProvider,
            NDataPumper.DataPumper dataPumper)
        {
            _actualityDatesProvider = actualityDatesProvider;
            _pumper = dataPumper;
            _configuration = new Configuration();
        }

        public async void RunJobs(string sourceProviderName, string sourceConnectionString, string targetProviderName, string targetConnectionString)
        {
            Log.Info("Performing synchronization for all jobs...");
            var configuration = new Configuration();
            var jobs = configuration.Jobs;

            BackgroundJob.Enqueue(() => ProcessInternal(jobs, sourceProviderName, sourceConnectionString, targetProviderName, targetConnectionString));
        }

        private async Task<IDataPumperSource> GetSourceProvider(string sourceProviderName, string sourceConnectionString)
        {
            if (sourceProviderName == "SQL")
            {
                var sourceProvider = new SqlDataPumperSourceTarget();
                await sourceProvider.Initialize(sourceConnectionString);
                return sourceProvider;
            }

            throw new ApplicationException($"No source provider with name '{sourceProviderName}'");
        }

        private async Task<IDataPumperTarget> GetTargetProvider(string targetProviderName, string targetConnectionString)
        {
            if (targetProviderName == "SQL")
            {
                var targetProvider = new SqlDataPumperSourceTarget();
                await targetProvider.Initialize(targetConnectionString);
                return targetProvider;
            }

            throw new ApplicationException($"No target provider with name '{targetProviderName}'");
        }

        [Queue("datapumper")]
        public async Task ProcessInternal(ConfigJobItem[] jobs, string sourceProviderName, string sourceConnectionString, string targetProviderName, string targetConnectionString)
        {
            Log.Warn("Started job to sync all tables...");

            var sourceProvider = await GetSourceProvider(sourceProviderName, sourceConnectionString);
            var targetProvider = await GetTargetProvider(targetProviderName, targetConnectionString);

            var tasks = jobs.ToList().Select(j => RunJobInternal(j, sourceProvider, targetProvider));
            var results = await Task.WhenAll(tasks);

            ResultWriteToFile(results);            
        }

        private void ResultWriteToFile(JobLog[] results)
        {
            Log.Trace(Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
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

        private async Task<JobLog> RunJobInternal(ConfigJobItem job, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Warn($"Processing {job.Name}");
            var log = new JobLog { Name = job.Name};

            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var records = await _pumper.Pump(sourceProvider, targetProvider,
                    new TableName(job.SourceTableName),
                    new TableName(job.TargetTableName), 
                    "ActualDate",
                    //new TableName(curDateTable), fullReload ? DateTime.Today.AddYears(-100) : job.Date);
                    new TableName("lr.VProperties"),
                    DateTime.Today.AddYears(-100));

                log.ElapsedTime = sw.Elapsed;
                log.RecordsProcessed = records;
                sw.Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing job {job}", ex);
                log.Error = ex.Message;
            }

            return log;
        }
    }
}
