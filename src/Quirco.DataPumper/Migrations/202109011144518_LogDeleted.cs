namespace Quirco.DataPumper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LogDeleted : DbMigration
    {
        public override void Up()
        {
            AddColumn("dp.JobLog", "RecordsDeleted", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dp.JobLog", "RecordsDeleted");
        }
    }
}
