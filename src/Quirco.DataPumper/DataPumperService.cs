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
        private readonly DataPumperConfiguration _configuration;
        private readonly SmtpSender _smtp;

        public EventHandler<ProgressEventArgs> Progress;

        public DataPumperService(NDataPumper.DataPumper dataPumper)
        {
            _pumper = dataPumper;
            _configuration = new DataPumperConfiguration();
            _smtp = new SmtpSender();
        }

        public async Task RunJob(PumperJobItem jobItem, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Info($"Performing synchronization for job '{jobItem.Name}'... ");
            await RunJobInternal(jobItem, sourceProvider, targetProvider);
        }

        public async Task RunJobs(IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Info("Performing synchronization for all jobs...");
            var configuration = new DataPumperConfiguration();
            var jobs = configuration.Jobs;
            await ProcessInternal(jobs, sourceProvider, targetProvider);
        }

        public async Task ProcessInternal(PumperJobItem[] jobs, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
        {
            Log.Warn("Started job to sync all tables...");

            var tasks = jobs.ToList().Select(j => RunJobInternal(j, sourceProvider, targetProvider));
            await Task.WhenAll(tasks);

            _smtp.SendEmailAsync();
        }

        private async Task RunJobInternal(PumperJobItem job, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider)
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

                    RunTargetSPBefore(job.PreRunStoreProcedureOnTarget, targetProvider);

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

                    RunTargetSPAfter(job.PostRunStoredProcedureOnTarget, targetProvider);
                    sw.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing job {job}", ex);
                    jobLog.Message = ex.Message;
                    jobLog.Status = SyncStatus.Error;

                    _smtp.JobLogs.Add(jobLog);
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
    }
}
