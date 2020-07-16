namespace Quirco.DataPumper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPreviousActualDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dp.TableSync", "PreviousActualDate", c => c.DateTime());
            DropColumn("dp.TableSync", "LastQuery");
        }
        
        public override void Down()
        {
            AddColumn("dp.TableSync", "LastQuery", c => c.String());
            DropColumn("dp.TableSync", "PreviousActualDate");
        }
    }
}
