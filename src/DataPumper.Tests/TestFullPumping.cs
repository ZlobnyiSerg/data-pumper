using System;
using System.Linq;
using System.Threading.Tasks;
using DataPumper.Core;
using DataPumper.Sql;
using FluentAssertions;
using NDataPumper = DataPumper.Core.DataPumper;
using Xunit;

namespace DataPumper.Tests
{
    [CollectionDefinition("Pumping tests", DisableParallelization = true)]
    public class TestFullPumping
    {
        private readonly TestSourceContext _source;
        private readonly TestTargetContext _target;

        public TestFullPumping()
        {
            _source = new TestSourceContext();
            _source.Database.EnsureDeleted();
            _source.Database.EnsureCreated();

            _target = new TestTargetContext();
            _target.Database.EnsureDeleted();
            _target.Database.EnsureCreated();
        }

        [Fact]
        public async Task TestSimplePumping()
        {
            // Arrange
            var source = new SqlDataPumperSourceTarget();
            await source.Initialize("Server=(local);Database=DataPumper.Test.Source;Integrated Security=True");

            var target = new SqlDataPumperSourceTarget();
            await target.Initialize("Server=(local);Database=DataPumper.Test.Target;Integrated Security=True");

            _source.Occupations.Add(new SourceOccupation("01.01.01", "01.01.01", "01.01.2001", 50));
            await _source.SaveChangesAsync();

            // Act
            var result = await new NDataPumper().Pump(source, target,
                new PumpParameters(
                    new DataSource("Occupations"),
                    new DataSource("Occupations"),
                    "ActualityDate",
                    new DateTime(2001, 01, 01),
                    new DateTime(2001, 01, 01)));

            // Assert
            result.Inserted.Should().Be(1);
            result.Deleted.Should().Be(0);
            _target.Occupations.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestSameDayPumping()
        {
            // Arrange
            var source = new SqlDataPumperSourceTarget();
            await source.Initialize("Server=(local);Database=DataPumper.Test.Source;Integrated Security=True");

            var target = new SqlDataPumperSourceTarget();
            await target.Initialize("Server=(local);Database=DataPumper.Test.Target;Integrated Security=True");

            _source.Occupations.Add(new SourceOccupation("01.01.01", "01.01.01", "01.01.2001", 50));
            await _source.SaveChangesAsync();

            // Act
            var result = await new NDataPumper().Pump(source, target,
                new PumpParameters(
                    new DataSource("Occupations"),
                    new DataSource("Occupations"),
                    "ActualityDate",
                    new DateTime(2001, 01, 01),
                    new DateTime(2001, 01, 01)));
            // Pump again
            result = await new NDataPumper().Pump(source, target,
                new PumpParameters(
                    new DataSource("Occupations"),
                    new DataSource("Occupations"),
                    "ActualityDate",
                    new DateTime(2001, 01, 01),
                    new DateTime(2001, 01, 01)));

            // Assert
            result.Inserted.Should().Be(1);
            result.Deleted.Should().Be(1);
            _target.Occupations.Should().HaveCount(1);
        }
    }
}