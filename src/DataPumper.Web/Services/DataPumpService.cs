using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataPumper.Core;
using DataPumper.Web.DataLayer;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataPumper.Web.Services
{
    public class DataPumpService
    {
        private readonly Core.DataPumper _pumper;
        private readonly DataPumperContext _context;
        private readonly ILogger<DataPumpService> _logger;
        public IEnumerable<IDataPumperSource> Sources { get; }
        public IEnumerable<IDataPumperTarget> Targets { get; }

        public DataPumpService(IEnumerable<IDataPumperSource> sources, 
            IEnumerable<IDataPumperTarget> targets, 
            DataPumper.Core.DataPumper pumper,
            DataPumperContext context,
            ILogger<DataPumpService> logger)
        {
            _pumper = pumper;
            _context = context;
            _logger = logger;
            Sources = sources;
            Targets = targets;
        }

        public EventHandler<ProgressEventArgs> Progress;

        public async Task Process(bool fullReload = false)
        {
            _logger.LogInformation($"Performing synchronization for all jobs...");
            BackgroundJob.Enqueue(() => ProcessInternal(fullReload, CancellationToken.None));
        }

        public async Task ProcessInternal(bool fullReload, CancellationToken token)
        {
            _logger.LogWarning("Started job to sync all tables...");
            foreach (var job in _context.TableSyncJobs)
            {
                await RunJobInternal(job, fullReload, token);
            }
        }

        private async Task RunJobInternal(TableSyncJob job, bool fullReload, CancellationToken token)
        {
            _logger.LogWarning($"Processing {job}");
            var log = new SyncJobLog
            {
                TableSyncJobId = job.Id,
                StartDate = DateTime.Now
            };
            _context.Logs.Add(log);
            await _context.SaveChangesAsync(token);
            
            try
            {
                var source = Sources.FirstOrDefault(s => s.GetName() == job.SourceProvider);
                if (source == null)
                    throw new ApplicationException($"No source provider with name '{job.SourceProvider}'");
                var target = Targets.FirstOrDefault(s => s.GetName() == job.TargetProvider);
                if (target == null)
                    throw new ApplicationException($"No target provider with name '{job.TargetProvider}'");

                await source.Initialize(job.SourceConnectionString);
                await target.Initialize(job.TargetConnectionString);

                var sw = new Stopwatch();
                sw.Start();

                target.Progress += (sender, args) =>
                {
                    log.Elapsed = sw.Elapsed;
                    log.RecordsProcessed = args.Processed;
                    _context.SaveChanges();

                    var handler = Progress;
                    handler?.Invoke(sender, args);
                };
                var records = await _pumper.Pump(source, target, new TableName(job.SourceTableName), new TableName(job.TargetTableName), "ActualDate", fullReload ? DateTime.Today.AddYears(-100) : job.Date);
                var currentDate = await GetCurrentDate(source) ?? DateTime.Now;
                
                job.Date = currentDate;
                _logger.LogInformation($"New job date is now '{job.Date}'");

                log.Elapsed = sw.Elapsed;
                log.EndDate = DateTime.Now;
                log.RecordsProcessed = records;
                log.Status = SyncStatus.Success;
                await _context.SaveChangesAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing job {job}", ex);
                
                log.EndDate = DateTime.Now;
                log.Status = SyncStatus.Error;
                log.Message = ex.Message;
                await _context.SaveChangesAsync(token);
            }
        }

        private async Task<DateTime?> GetCurrentDate(IDataPumperSource source)
        {
            var curDateTable = (await _context.Settings.FirstOrDefaultAsync(s => s.Key == Setting.CurrentDateTable))?.Value;
            var curDateField = (await _context.Settings.FirstOrDefaultAsync(s => s.Key == Setting.CurrentDateField))?.Value;
            if (string.IsNullOrEmpty(curDateTable) || string.IsNullOrEmpty(curDateField))
                return null;
            return await source.GetCurrentDate(new TableName(curDateTable), curDateField);
        }
    }
}