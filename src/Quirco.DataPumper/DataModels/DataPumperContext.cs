using System.Data.Entity;
using Quirco.DataPumper.Migrations;

namespace Quirco.DataPumper.DataModels
{
    public class DataPumperContext : DbContext
    {
        public DbSet<TableSync> TableSyncs { get; set; }

        public DbSet<JobLog> Logs { get; set; }

        public DataPumperContext(string connectionString) : base(connectionString)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataPumperContext, Configuration>());
        }

        public DataPumperContext() : this(
            ConfigurationManager.Configuration.GetRequiredWithFallback("Core:MetadataConnectionString", "Core:ConnectionString"))
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TableSync>().HasIndex(e => e.CreatedDate);
        }
    }
}
