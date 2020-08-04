using Common.Logging;
using DataPumper.Core;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Quirco.DataPumper.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDataPumper = DataPumper.Core;

namespace Quirco.DataPumper
{
    public class DataPumperService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperService));

        private readonly NDataPumper.DataPumper _pumper;
        private readonly DPConfiguration _configuration;

        public EventHandler<ProgressEventArgs> Progress;

        public DataPumperService(NDataPumper.DataPumper dataPumper)
        {
            _pumper = dataPumper;
            _configuration = new DPConfiguration();
        }

        public async Task RunJob(ConfigJobItem jobItem, IDataPumperProvider sourceProvider, IDataPumperProvider targetProvider)
        {
            Log.Info($"Performing synchronization for job '{jobItem.Name}'... ");
            Validation(sourceProvider, targetProvider);

            var dataPumperSource = sourceProvider as IDataPumperSource;
            var dataPumperTarget = targetProvider as IDataPumperTarget;
            await RunJobInternal(jobItem, dataPumperSource, dataPumperTarget);
        }

        public async Task RunJobs(IDataPumperProvider sourceProvider, IDataPumperProvider targetProvider)
        {
            Log.Info("Performing synchronization for all jobs...");
            var configuration = new DPConfiguration();
            var jobs = configuration.Jobs;
            Validation(sourceProvider, targetProvider);

            var dataPumperSource = sourceProvider as IDataPumperSource;
            var dataPumperTarget = targetProvider as IDataPumperTarget;
            await ProcessInternal(jobs, dataPumperSource, dataPumperTarget);
        }

        public async Task ProcessInternal(ConfigJobItem[] jobs, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Warn("Started job to sync all tables...");

            var tasks = jobs.ToList().Select(j => RunJobInternal(j, sourceProvider, targetProvider));
            await Task.WhenAll(tasks);   
        }

        private async Task RunJobInternal(ConfigJobItem job, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Warn($"Processing {job.Name}");
            using (var ctx = new DataPumperContext())
            {
                var tableSync = await ctx.TableSyncs.FirstOrDefaultAsync(ts => ts.TableName == job.TargetTableName);
                if (tableSync == null)
                {
                    tableSync = new TableSync
                    {
                        TableName = job.TargetTableName
                    };
                    ctx.TableSyncs.Add(tableSync);
                }

                var jobLog = new JobLog { TableSync = tableSync };
                ctx.Logs.Add(jobLog);

                await ctx.SaveChangesAsync();

                try
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    targetProvider.Progress += (sender, args) =>
                    {
                        jobLog.RecordsProcessed = args.Processed;
                        ctx.SaveChanges();

                        var handler = Progress;
                        handler?.Invoke(sender, args);
                    };

                    RunTargetSPBefore(job.TargetSPQueryBefore, targetProvider);

                    var jobActualDate = tableSync.ActualDate; // Если переливка не выполнялась, то будет Null
                    var onDate = jobActualDate == null ? DateTime.Today.AddYears(-100) : jobActualDate.Value;

                    var currentDate = await sourceProvider.GetCurrentDate(_configuration.CurrentDateQuery) ?? DateTime.Now.Date;

                    if (job.HistoricMode && currentDate == tableSync.ActualDate)
                    {
                        onDate = tableSync.PreviousActualDate == null ? DateTime.Today.AddYears(-100) : tableSync.PreviousActualDate.Value;
                    }
                    else if (job.HistoricMode)
                    {
                        tableSync.PreviousActualDate = tableSync.ActualDate;
                    }

                    var records = await _pumper.Pump(sourceProvider, targetProvider,
                        new TableName(job.SourceTableName),
                        new TableName(job.TargetTableName),
                        _configuration.ActualityColumnName,
                        _configuration.HistoricColumnFrom,
                        new TableName(_configuration.Properties), // Таблица, где хранятся объекты
                        onDate,
                        job.HistoricMode,
                        currentDate);

                    tableSync.ActualDate = currentDate;
                    jobLog.EndDate = DateTime.Now;
                    jobLog.RecordsProcessed = records;
                    jobLog.Status = SyncStatus.Success;

                    RunTargetSPAfter(job.TargetSPQueryAfter, targetProvider);
                    sw.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing job {job}", ex);
                    jobLog.Message = ex.Message;
                    jobLog.Status = SyncStatus.Error;
                }

                await ctx.SaveChangesAsync();
            }
        }

        private void RunTargetSPBefore(string targetSPQueryBefore, IDataPumperTarget targetProvider)
        {
            targetProvider.RunStoredProcedure(targetSPQueryBefore);
        }

        private void RunTargetSPAfter(string targetSPQueryAfter, IDataPumperTarget targetProvider)
        {
            targetProvider.RunStoredProcedure(targetSPQueryAfter);
        }

        private void Validation(IDataPumperProvider sourceProvider, IDataPumperProvider targetProvider)
        {
            ValidateProviders(sourceProvider, targetProvider);

            if (string.IsNullOrEmpty(_configuration.Properties))
                throw new ApplicationException($"Required set 'Properties' in data-pumper.config");

            if (string.IsNullOrEmpty(_configuration.ActualityColumnName))
                throw new ApplicationException($"Required set 'ActualityColumnName' in data-pumper.config");

            if (string.IsNullOrEmpty(_configuration.HistoricColumnFrom))
                throw new ApplicationException($"Required set 'HistoricColumns.From' in data-pumper.config");

            if (string.IsNullOrEmpty(_configuration.HistoricColumnTo))
                throw new ApplicationException($"Required set 'HistoricColumns.To' in data-pumper.config");

            if (string.IsNullOrEmpty(_configuration.CurrentDateQuery))
                throw new ApplicationException($"Required set 'CurrentDateQuery' in data-pumper.config");
        }

        private static void ValidateProviders(IDataPumperProvider sourceProvider, IDataPumperProvider targetProvider)
        {
            if (!(sourceProvider is IDataPumperSource))
                throw new ApplicationException($"Source provider '{sourceProvider.GetName()}' is not IDataPumperSource");

            if (!(targetProvider is IDataPumperTarget))
                throw new ApplicationException($"Target provider '{targetProvider.GetName()}' is not IDataPumperTarget");
        }
    }
}
