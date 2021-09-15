
using System.Data.Entity.Migrations;
using Quirco.DataPumper.DataModels;

namespace Quirco.DataPumper.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<DataPumperContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(DataPumperContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
        }
    }
}
