using System;
using Microsoft.EntityFrameworkCore;

namespace DataPumper.Tests
{
    public class TestTargetContext : DbContext
    {
        public DbSet<TargetHistoricalOccupation> Occupations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(local);Database=DataPumper.Test.Target;Integrated Security=True");
            base.OnConfiguring(optionsBuilder);
        }
    }
}