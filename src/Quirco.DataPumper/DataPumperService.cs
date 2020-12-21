using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using DataPumper.Core;
using Microsoft.Extensions.Configuration;
using Quirco.DataPumper.DataModels;
using NDataPumper = DataPumper.Core;

namespace Quirco.DataPumper
{
    public class DataPumperService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperService));

        private readonly NDataPumper.DataPumper _pumper;
        private readonly DataPumperConfiguration _configuration;
        private readonly string[] _tenantCodes;

        public EventHandler<NDataPumper.ProgressEventArgs> Progress;
        public ILogsSender LogsSender { get; }

        public DataPumperService(DataPumperConfiguration configuration)
        {
            _pumper = new NDataPumper.DataPumper();
            _configuration = configuration;
            LogsSender = new SmtpSender(configuration);
        }

        public DataPumperService(DataPumperConfiguration configuration, string[] tenantCodes) : this(configuration)
        {
            _tenantCodes = tenantCodes;
        }

        public async Task RunJob(PumperJobItem jobItem, NDataPumper.IDataPumperSource sourceProvider, NDataPumper.IDataPumperTarget targetProvider, bool fullReloading = false)
        {
            Log.Info($"Performing synchronization for job '{jobItem.Name}'... ");
            var log = await RunJobInternal(jobItem, sourceProvider, targetProvider, fullReloading);
            LogsSender.Send(new[] { log });
        }

        public async Task RunJobs(NDataPumper.IDataPumperSource sourceProvider, NDataPumper.IDataPumperTarget targetProvider, bool fullReloading = false)
        {
            Log.Info("Running all jobs...");
            var jobs = _configuration.Jobs;
            var logs = await ProcessInternal(jobs, sourceProvider, targetProvider, fullReloading);
            LogsSender.Send(logs.ToList());
        }

        public async Task<IEnumerable<JobLog>> ProcessInternal(PumperJobItem[] jobs, NDataPumper.IDataPumperSource sourceProvider, NDataPumper.IDataPumperTarget targetProvider,
            bool fullReloading)
        {
            Log.Warn("Started job to sync all tables...");

            var jobLogs = new List<JobLog>();

            foreach (var job in jobs)
            {
                var jobLog = await RunJobInternal(job, sourceProvider, targetProvider, fullReloading);
                jobLogs.Add(jobLog);
            }

            return jobLogs;
        }

        private async Task<JobLog> RunJobInternal(PumperJobItem job, NDataPumper.IDataPumperSource sourceProvider, NDataPumper.IDataPumperTarget targetProvider, bool fullReloading)
        {
            Log.Warn($"Processing {job.Name}");
            using (var ctx = new DataPumperContext(_configuration.MetadataConnectionString))
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

                var jobLog = new JobLog {TableSync = tableSync};
                ctx.Logs.Add(jobLog);
                await ctx.SaveChangesAsync();

                try
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    targetProvider.Progress += UpdateJobLog;

                    await targetProvider.RunQuery(job.PreRunQuery);

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
                        new NDataPumper.PumpParameters(
                            new NDataPumper.TableName(job.SourceTableName),
                            new NDataPumper.TableName(job.TargetTableName),
                            _configuration.ActualityColumnName,
                            _configuration.HistoricColumnFrom,
                            _configuration.TenantField,
                            onDate,
                            job.HistoricMode,
                            currentDate,
                            fullReloading,
                            _tenantCodes));

                    tableSync.ActualDate = currentDate;
                    jobLog.EndDate = DateTime.Now;
                    jobLog.RecordsProcessed = records;
                    jobLog.Status = SyncStatus.Success;
                    await targetProvider.RunQuery(job.PostRunQuery);
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

        private void UpdateJobLog(object sender, ProgressEventArgs args)
        {
            using (var logContext = new DataPumperContext(_configuration.MetadataConnectionString))
            {
                var logRecord = GetJobLog(args.TableName, logContext);
                logRecord.RecordsProcessed = args.Processed;
                logContext.SaveChanges();
            }
        }

        private JobLog GetJobLog(TableName tableName, DataPumperContext dataPumperContext)
        {
            var tableSync = dataPumperContext.TableSyncs.FirstOrDefault(ts => ts.TableName == tableName.SourceFullName);
            var jobLog = dataPumperContext.Logs.OrderByDescending(l => l.Id).FirstOrDefault(l => l.TableSyncId == tableSync.Id);
            return jobLog;
        }

        public Task<List<JobLog>> GetLogRecords(int skip, int take)
        {
            using (var ctx = new DataPumperContext(_configuration.MetadataConnectionString))
            {
                return ctx.Logs.Skip(skip).Take(take).ToListAsync();
            }
        } 
    }
}