using System;
using Microsoft.EntityFrameworkCore;

namespace DataPumper.Tests
{
    public class TestSourceContext : DbContext
    {
        public DbSet<SourceOccupation> Occupations { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(local);Database=DataPumper.Test.Source;Integrated Security=True");
            base.OnConfiguring(optionsBuilder);
        }
    }
}