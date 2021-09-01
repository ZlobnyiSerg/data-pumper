using System;
using System.Collections;
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

        private readonly NDataPumper.DataPumper _pumper;
        private readonly DataPumperConfiguration _configuration;
        private readonly string[] _tenantCodes;
        public ILogsSender LogsSender { get; set; }

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

        /// <summary>
        /// Осуществляет частичное обновление всех таблиц, где есть колонка из filter.FieldName со значением filter.Values
        /// </summary>
        public async Task<PumpResult> PartialLoad(
            IDataPumperSource sourceProvider,
            IDataPumperTarget targetProvider,
            PartialLoadRequest request)
        {
            var deleted = 0L;
            var inserted = 0L;

            var processedJobs = new HashSet<string>();
            foreach (var filter in request.Filters)
            {
                Log.Warn($"Performing partial update for '{filter.FieldName}' IN ({string.Join(", ", filter.Values)})");
                var jobs = await GetJobsWithField(sourceProvider, filter.FieldName);
                foreach (var job in jobs.Where(j=>!processedJobs.Contains(j.Name)))
                {
                    Log.Debug($"Updating table '{job.SourceTableName}'...");
                    var res = await _pumper.Pump(sourceProvider, targetProvider, new PumpParameters(
                        new TableName(job.SourceTableName),
                        new TableName(job.TargetTableName),
                        _configuration.ActualityColumnName,
                        _configuration.HistoricColumnFrom,
                        _configuration.TenantField,
                        request.ActualDate,
                        false,
                        request.ActualDate,
                        false,
                        null
                    )
                    {
                        Filter = filter
                    });
                    deleted += res.Deleted;
                    inserted += res.Inserted;
                    processedJobs.Add(job.Name);
                }
            }

            return new PumpResult(inserted, deleted);
        }
        
        private readonly Dictionary<string, List<PumperJobItem>> _jobsCacheByFieldName = new();

        /// <summary>
        /// Возвращает список джобов, в исходных таблицах которых есть поле с указанным названием.
        /// Нужно для синхронизации данных "на лету".
        /// </summary>
        /// <param name="sourceProvider"></param>
        /// <param name="fieldName"></param>
        private async Task<IEnumerable<PumperJobItem>> GetJobsWithField(IDataPumperSource sourceProvider, string fieldName)
        {
            if (!_jobsCacheByFieldName.TryGetValue(fieldName, out var jobs))
            {
                jobs = new List<PumperJobItem>();
                foreach (var job in _configuration.Jobs)
                {
                    var fields = await sourceProvider.GetTableFields(new TableName(job.SourceTableName));
                    if (fields.Contains(fieldName))
                        jobs.Add(job);
                }

                _jobsCacheByFieldName[fieldName] = jobs;
            }
            
            return jobs;
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

                await targetProvider.RunQuery(job.PreRunQuery);

                if (fullReloading)
                {
                    // При полной перезаливке обнуляем ActualDate
                    tableSync.ActualDate = null;
                }

                var jobActualDate = tableSync.ActualDate; // Если переливка не выполнялась, то будет Null
                var onDate = jobActualDate ?? DateTime.Today.AddYears(-100);

                var currentDate = await sourceProvider.GetCurrentDate(_configuration.CurrentDateQuery) ?? DateTime.Now.Date;

                if (job.HistoricMode)
                {
                    if (currentDate == tableSync.ActualDate)
                    {
                        onDate = tableSync.PreviousActualDate ?? DateTime.Today.AddYears(-100);
                    }
                    else
                    {
                        tableSync.PreviousActualDate = tableSync.ActualDate;
                    }
                }

                var records = await _pumper.Pump(sourceProvider, targetProvider,
                    new PumpParameters(
                        new TableName(job.SourceTableName),
                        new TableName(job.TargetTableName),
                        _configuration.ActualityColumnName,
                        _configuration.HistoricColumnFrom,
                        _configuration.TenantField,
                        onDate.AddDays(_configuration.BackwardReloadDays),
                        job.HistoricMode,
                        currentDate,
                        fullReloading,
                        _tenantCodes)
                    {
                        DeleteProtectionDate = _configuration.DeleteProtectionDate
                    });

                tableSync.ActualDate = currentDate;
                jobLog.EndDate = DateTime.Now;
                jobLog.RecordsProcessed = records.Inserted;
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

        private void UpdateJobLog(object sender, ProgressEventArgs args)
        {
            using var logContext = new DataPumperContext(_configuration.MetadataConnectionString);
            var logRecord = GetJobLog(args.TableName, logContext);
            logRecord.RecordsProcessed = args.Processed;
            logContext.SaveChanges();
        }

        private JobLog GetJobLog(TableName tableName, DataPumperContext dataPumperContext)
        {
            var tableSync = dataPumperContext.TableSyncs.FirstOrDefault(ts => ts.TableName == tableName.SourceFullName);
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