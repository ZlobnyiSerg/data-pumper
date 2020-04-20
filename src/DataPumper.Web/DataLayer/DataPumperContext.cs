using System.Linq;
using DataPumper.Sql;
using Microsoft.EntityFrameworkCore;

namespace DataPumper.Web.DataLayer
{
    public class DataPumperContext : DbContext
    {
        public DbSet<TableSyncJob> TableSyncJobs { get; set; }
        public DbSet<SyncJobLog> Logs { get; set; }

        public DbSet<Setting> Settings { get; set; }


        public DataPumperContext()
        {
        }

        public DataPumperContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TableSyncJob>().HasKey(t => t.Id);
            modelBuilder.Entity<TableSyncJob>().HasMany(t=>t.Log).WithOne(l=>l.TableSyncJob).OnDelete(DeleteBehavior.Cascade);
            
            
            modelBuilder.Entity<Setting>().HasKey(e => e.Key);

            modelBuilder.Entity<SyncJobLog>().HasKey(j => j.Id);
        }

        public void Seed()
        {
            if (!TableSyncJobs.Any())
            {
                TableSyncJobs.AddRange(new TableSyncJob
                {
                    SourceProvider = SqlDataPumperSourceTarget.Name,
                    SourceConnectionString =
                        "Server=(local);Database=Logus.HMS;Integrated Security=true;MultipleActiveResultSets=true;Application Name=DataPumper",
                    SourceTableName = "lr.VTransactions",
                    TargetProvider = SqlDataPumperSourceTarget.Name,
                    TargetConnectionString =
                        "Server=(local);Database=Logus.Reporting;Integrated Security=true;MultipleActiveResultSets=true;Application Name=DataPumper",
                    TargetTableName = "lr.Transactions"
                });
                
                Settings.AddRange(new Setting
                {
                    Key = Setting.CurrentDateTable,
                    Value = "lr.Properties"
                }, new Setting
                {
                    Key = Setting.CurrentDateField,
                    Value = "CurrentDate"
                }, new Setting
                {
                    Key = Setting.Cron,
                    Value = "0 30 3 ? * *"
                });
                SaveChanges();
            }
        }
    }
}