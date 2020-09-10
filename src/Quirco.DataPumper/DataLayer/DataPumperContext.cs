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
        public static DataPumperConfiguration configuration = new DataPumperConfiguration();

        public DbSet<TableSync> TableSyncs { get; set; }

        public DbSet<JobLog> Logs { get; set; }

        public DataPumperContext() : this("Server=FIUTEE_HOME\\SQLEXPRESS;Database=Logus.HMS;Integrated Security=true;MultipleActiveResultSets=true;Application Name=Logus")
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
