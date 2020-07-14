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

        private readonly IActualityDatesProvider _actualityDatesProvider;
        private readonly NDataPumper.DataPumper _pumper;
        private readonly DPConfiguration _configuration;

        public DataPumperService(IActualityDatesProvider actualityDatesProvider,
            NDataPumper.DataPumper dataPumper)
        {
            _actualityDatesProvider = actualityDatesProvider;
            _pumper = dataPumper;
            _configuration = new DPConfiguration();
        }

        public async Task RunJobs(IDataPumperProvider sourceProvider, IDataPumperProvider targetProvider)
        {
            Log.Info("Performing synchronization for all jobs...");
            using (var ctx = new DataPumperContext())
            {
                ctx.Database.Initialize(false);
            }
            var configuration = new DPConfiguration();
            var jobs = configuration.Jobs;
            if (!(sourceProvider is IDataPumperSource))
                throw new ApplicationException($"Source provider '{sourceProvider.GetName()}' is not IDataPumperSource");

            if (!(targetProvider is IDataPumperTarget))
                throw new ApplicationException($"Target provider '{targetProvider.GetName()}' is not IDataPumperTarget");

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

                var jobLog = new DataPumperLogEntry();
                ctx.Logs.Add(jobLog);

                await ctx.SaveChangesAsync();

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

                    tableSync.ActualDate = currentDate;
                    jobLog.EndDate = DateTime.Now;
                    jobLog.RecordsProcessed = records;
                    jobLog.Status = SyncStatus.Success;
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
    }
}
