namespace Quirco.DataPumper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dp.JobLog",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TableSyncId = c.Long(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(),
                        RecordsProcessed = c.Long(nullable: false),
                        Message = c.String(),
                        Status = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dp.TableSync", t => t.TableSyncId, cascadeDelete: true)
                .Index(t => t.TableSyncId);
            
            CreateTable(
                "dp.TableSync",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TableName = c.String(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        ActualDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.CreatedDate);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dp.JobLog", "TableSyncId", "dp.TableSync");
            DropIndex("dp.TableSync", new[] { "CreatedDate" });
            DropIndex("dp.JobLog", new[] { "TableSyncId" });
            DropTable("dp.TableSync");
            DropTable("dp.JobLog");
        }
    }
}
