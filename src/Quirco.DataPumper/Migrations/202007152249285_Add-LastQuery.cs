namespace Quirco.DataPumper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLastQuery : DbMigration
    {
        public override void Up()
        {
            AddColumn("dp.TableSync", "LastQuery", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dp.TableSync", "LastQuery");
        }
    }
}
