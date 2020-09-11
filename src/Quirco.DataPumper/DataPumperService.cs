using Common.Logging;
using Common.Logging.Configuration;
using DataPumper.Core;
using Hangfire;
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
        private readonly string[] _tenantCodes;

        public EventHandler<ProgressEventArgs> Progress;

        public DataPumperService(NDataPumper.DataPumper dataPumper)
        {
            _pumper = dataPumper;
            _configuration = new DataPumperConfiguration();
            _smtp = new SmtpSender();            
        }

        public DataPumperService(NDataPumper.DataPumper dataPumper, string[] tenantCodes) : this(dataPumper)
        {
            _tenantCodes = tenantCodes;
        }

        public async Task RunJob(PumperJobItem jobItem, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider, bool fullReloading = false)
        {
            Log.Info($"Performing synchronization for job '{jobItem.Name}'... ");
            await RunJobInternal(jobItem, sourceProvider, targetProvider, fullReloading);
        }

        public async Task RunJobs(IDataPumperSource sourceProvider, IDataPumperTarget targetProvider, bool fullReloading = false)
        {
            Log.Info("Performing synchronization for all jobs...");
            var configuration = new DataPumperConfiguration();
            var jobs = configuration.Jobs;
            var logs = await ProcessInternal(jobs, sourceProvider, targetProvider, fullReloading);

            BackgroundJob.Enqueue(() => 
                _smtp.SendEmailAsync(logs.Where(l => l.Status == SyncStatus.Error)));
        }

        public async Task<IEnumerable<JobLog>> ProcessInternal(PumperJobItem[] jobs, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider, bool fullReloading)
        {
            Log.Warn("Started job to sync all tables...");

            var jobLogs = new List<JobLog>();

            foreach(var job in jobs)
            {
                var jobLog = await RunJobInternal(job, sourceProvider, targetProvider, fullReloading);
                jobLogs.Add(jobLog);
            }

            return jobLogs;
        }

        private async Task<JobLog> RunJobInternal(PumperJobItem job, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider, bool fullReloading)
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

                    if (fullReloading)
                    {
                        // При полной перезаливке обнуляем ActualDate
                        tableSync.ActualDate = null;
                    }

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
                        _configuration.TenantField,
                        onDate,
                        job.HistoricMode,
                        currentDate,
                        fullReloading,
                        _tenantCodes);

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
                }

                await ctx.SaveChangesAsync();

                return jobLog;
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
