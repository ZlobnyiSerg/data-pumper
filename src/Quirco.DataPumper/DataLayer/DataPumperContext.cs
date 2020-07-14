using Common.Logging;
using Quirco.DataPumper.Migrations;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quirco.DataPumper.DataLayer
{
    public class DataPumperContext : DbContext
    {
        private static DPConfiguration configuration = new DPConfiguration();

        public DbSet<TableSync> TableSyncs { get; set; }

        public DbSet<DataPumperLogEntry> Logs { get; set; }

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
