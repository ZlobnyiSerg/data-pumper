using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Quirco.DataPumper.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<DataLayer.DataPumperContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(DataLayer.DataPumperContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
        }
    }
}
