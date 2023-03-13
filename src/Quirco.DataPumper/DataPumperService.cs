using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using DataPumper.Core;
using Quirco.DataPumper.DataModels;
using NDataPumper = DataPumper.Core;

namespace Quirco.DataPumper
{
    public class DataPumperService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperService));
        
        private readonly DataPumperConfiguration _configuration;
        private readonly string[] _tenantCodes;
        public ILogsSender LogsSender { get; set; }

        public DataPumperService(DataPumperConfiguration configuration)
        {
            _configuration = configuration;
            LogsSender = new SmtpSender(configuration);
        }

        public DataPumperService(DataPumperConfiguration configuration, string[] tenantCodes) : this(configuration)
        {
            _tenantCodes = tenantCodes;
        }

        public async Task RunJob(PumperJobItem jobItem, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider, bool fullReloading = false)
        {
            Log.Info($"Performing synchronization for job '{jobItem.Name}'... ");
            var log = await RunJobInternal(jobItem, sourceProvider, targetProvider, fullReloading);
            LogsSender.Send(new[] { log });
        }

        public async Task RunJobs(IDataPumperSource sourceProvider, IDataPumperTarget targetProvider, bool fullReloading = false)
        {
            Log.Info("Running all jobs...");
            var jobs = _configuration.Jobs;
            var logs = await ProcessInternal(jobs, sourceProvider, targetProvider, fullReloading);
            LogsSender.Send(logs.ToList());
        }

        public async Task<IEnumerable<JobLog>> ProcessInternal(IEnumerable<PumperJobItem> jobs, IDataPumperSource sourceProvider,
            IDataPumperTarget targetProvider,
            bool fullReloading)
        {
            Log.Warn("Started job to sync all tables...");

            var jobLogs = new List<JobLog>();

            foreach (var job in jobs)
            {
                var jobLog = await RunJobInternal(job, sourceProvider, targetProvider, fullReloading);
                jobLogs.Add(jobLog);
            }

            Log.Info("Transfer summary (table, deleted, inserted):");
            foreach (var jobLog in jobLogs)
            {
                Log.Info($"{jobLog.TableSync.TableName,-30}{jobLog.RecordsDeleted,10}{jobLog.RecordsProcessed,10}");
            }

            return jobLogs;
        }

        private async Task<JobLog> RunJobInternal(PumperJobItem job, IDataPumperSource sourceProvider, IDataPumperTarget targetProvider, bool fullReloading)
        {
            Log.Warn($"Processing {job.Name}");
            using var ctx = new DataPumperContext(_configuration.MetadataConnectionString);
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

                targetProvider.Progress += UpdateJobLog;

                await targetProvider.ExecuteRawQuery(job.PreRunQuery);

                if (fullReloading)
                {
                    // При полной перезаливке обнуляем ActualDate
                    tableSync.ActualDate = null;
                }

                var jobActualDate = tableSync.ActualDate; // Если переливка не выполнялась, то будет Null
                var onDate = jobActualDate ?? DateTime.Today.AddYears(-100);

                var currentDate = await sourceProvider.GetCurrentDate(_configuration.CurrentDateQuery) ?? DateTime.Today;

                IDataPumper pumper = job.HistoricMode ? new HistoricDataPumper() : new NDataPumper.DataPumper();

                var records = await pumper.Pump(sourceProvider, targetProvider,
                    new PumpParameters(
                        new DataSource(job.SourceTableName, job.StoredProcedure),
                        new DataSource(job.TargetTableName),
                        _configuration.ActualityColumnName,
                        onDate.AddDays(_configuration.BackwardReloadDays),
                        jobActualDate,
                        currentDate,
                        _configuration.HistoricColumnsFrom,
                        _configuration.HistoricColumnsTo,
                        fullReloading,
                        _configuration.TenantField,
                        _tenantCodes)
                    {
                        DeleteProtectionDate = _configuration.DeleteProtectionDate
                    });

                tableSync.ActualDate = currentDate;
                jobLog.EndDate = DateTime.Now;
                jobLog.RecordsProcessed = records.Inserted;
                jobLog.RecordsDeleted = records.Deleted;
                jobLog.Status = SyncStatus.Success;
                await targetProvider.ExecuteRawQuery(job.PostRunQuery);
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

        private void UpdateJobLog(object sender, ProgressEventArgs args)
        {
            using var logContext = new DataPumperContext(_configuration.MetadataConnectionString);
            var logRecord = GetJobLog(args.DataSource, logContext);
            logRecord.RecordsProcessed = args.Processed;
            logContext.SaveChanges();
        }

        private JobLog GetJobLog(DataSource dataSource, DataPumperContext dataPumperContext)
        {
            var tableSync = dataPumperContext.TableSyncs.FirstOrDefault(ts => ts.TableName == dataSource.SourceFullName);
            var jobLog = dataPumperContext.Logs.OrderByDescending(l => l.Id).FirstOrDefault(l => l.TableSyncId == tableSync.Id);
            return jobLog;
        }

        public async Task<List<JobLog>> GetLogRecords(int skip, int take)
        {
            using var ctx = new DataPumperContext(_configuration.MetadataConnectionString);
            var logs = await ctx.Logs
                .Include(l => l.TableSync)
                .OrderByDescending(r => r.StartDate)
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return logs;
        }
    }
}