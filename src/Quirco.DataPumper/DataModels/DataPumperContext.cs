using System.Data.Entity;
using Quirco.DataPumper.Migrations;

namespace Quirco.DataPumper.DataModels
{
    public class DataPumperContext : DbContext
    {
        private static DataPumperConfiguration configuration = new DataPumperConfiguration();

        public DbSet<TableSync> TableSyncs { get; set; }

        public DbSet<JobLog> Logs { get; set; }

        public DataPumperContext() : this(configuration.ConnectionString)
        {
        }

        public DataPumperContext(string connectionString) : base(connectionString)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataPumperContext, Configuration>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TableSync>().HasIndex(e => e.CreatedDate);
        }
    }
}
