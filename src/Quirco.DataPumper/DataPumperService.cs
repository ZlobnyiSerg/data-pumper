using Common.Logging;
using DataPumper.Core;
using DataPumper.Sql;
using Hangfire;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NDataPumper = DataPumper.Core;

namespace Quirco.DataPumper
{
    public class DataPumperService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperService));

        private readonly IActualityDatesProvider _actualityDatesProvider;
        private readonly NDataPumper.DataPumper _pumper;

        public DataPumperService(IActualityDatesProvider actualityDatesProvider,
            NDataPumper.DataPumper dataPumper)
        {
            _actualityDatesProvider = actualityDatesProvider;
            _pumper = dataPumper;
        }

        public async void RunJobs(IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Info("Performing synchronization for all jobs...");
            var configuration = new Configuration();
            var jobs = configuration.Jobs;
            BackgroundJob.Enqueue(() => ProcessInternal(jobs, sourceProvider, targetProvider));
        }

        public async Task ProcessInternal(ConfigJobItem[] jobs, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Warn("Started job to sync all tables...");

            if (sourceProvider == null)
                throw new ApplicationException($"No source provider");

            if (targetProvider == null)
                throw new ApplicationException($"No target provider");

            var logs = new List<JobLog>();

            foreach (var job in jobs)
            {
                await RunJobInternal(job, sourceProvider, targetProvider);
            }
        }

        private async Task<JobLog> RunJobInternal(ConfigJobItem job, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Warn($"Processing {job.Name}");
            var log = new JobLog();

            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var curDateTable = (await _context.Settings.FirstOrDefaultAsync(s => s.Key == Setting.CurrentDateTable, token))?.Value;
                var records = await _pumper.Pump(sourceProvider, targetProvider,
                    new TableName(job.SourceTableName),
                    new TableName(job.TargetTableName), 
                    "ActualDate",
                    //new TableName(curDateTable), fullReload ? DateTime.Today.AddYears(-100) : job.Date);
                    new TableName(curDateTable),
                    _actualityDatesProvider.GetJobActualDate(job.Name));

                log.ElapsedTime = sw.Elapsed;
                log.RecordsProcessed = records;
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
