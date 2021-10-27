using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DataPumper.Core;
using DataPumper.Sql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NDataPumper = DataPumper.Core.DataPumper;
using Xunit;

namespace DataPumper.Tests
{
    [CollectionDefinition("Pumping tests", DisableParallelization = true)]
    public class TestHistoricalPumping
    {
        private readonly TestSourceContext _sourceContext;
        private readonly TestTargetContext _targetContext;
        private readonly HistoricDataPumper _pumper;

        public TestHistoricalPumping()
        {
            _sourceContext = new TestSourceContext();
            _sourceContext.Database.EnsureDeleted();
            _sourceContext.Database.EnsureCreated();

            _targetContext = new TestTargetContext();
            _targetContext.Database.EnsureDeleted();
            _targetContext.Database.EnsureCreated();

            _pumper = new HistoricDataPumper();
        }

        [Fact]
        public async Task TestHistoricalRecords()
        {
            // https://docs.google.com/spreadsheets/d/10XuuhYfnVFrD8i8mdu6jlqgCyR8CGbP7tezgPeYDDp4/edit#gid=1602361020
            var testCase = new HistoricTestCase
            {
                Pumps =
                {
                    new HistoricTestCase.TargetDayStats("01.05.21")
                    {
                        SourceData =
                        {
                            new SourceOccupation("01.05.21", "01.05.21", "01.05.2021", 80),
                            new SourceOccupation("02.05.21", "01.05.21", "01.05.2021", 50),
                            new SourceOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new SourceOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new SourceOccupation("05.05.21", "01.05.21", "01.05.2021", 20),
                            new SourceOccupation("06.05.21", "01.05.21", "01.05.2021", 15)
                        },
                        ExpectedData =
                        {
                            new TargetHistoricalOccupation("01.05.21", "01.05.21", "01.05.2021", 80),
                            new TargetHistoricalOccupation("02.05.21", "01.05.21", "01.05.2021", 50),
                            new TargetHistoricalOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new TargetHistoricalOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new TargetHistoricalOccupation("05.05.21", "01.05.21", "01.05.2021", 20),
                            new TargetHistoricalOccupation("06.05.21", "01.05.21", "01.05.2021", 15)
                        }
                    },
                    new HistoricTestCase.TargetDayStats("02.05.21")
                    {
                        SourceData =
                        {
                            new SourceOccupation("01.05.21", "02.05.21", "01.01.2200", 80),
                            new SourceOccupation("02.05.21", "02.05.21", "02.05.2021", 60),
                            new SourceOccupation("03.05.21", "02.05.21", "02.05.2021", 60),
                            new SourceOccupation("04.05.21", "02.05.21", "02.05.2021", 55),
                            new SourceOccupation("05.05.21", "02.05.21", "02.05.2021", 30),
                            new SourceOccupation("06.05.21", "02.05.21", "02.05.2021", 20)
                        },
                        ExpectedData =
                        {
                            new TargetHistoricalOccupation("01.05.21", "01.05.21", "01.01.2200", 80),
                            new TargetHistoricalOccupation("02.05.21", "01.05.21", "01.05.2021", 50),
                            new TargetHistoricalOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new TargetHistoricalOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new TargetHistoricalOccupation("05.05.21", "01.05.21", "01.05.2021", 20),
                            new TargetHistoricalOccupation("06.05.21", "01.05.21", "01.05.2021", 15),
                            
                            new TargetHistoricalOccupation("02.05.21", "02.05.21", "02.05.2021", 60),
                            new TargetHistoricalOccupation("03.05.21", "02.05.21", "02.05.2021", 60),
                            new TargetHistoricalOccupation("04.05.21", "02.05.21", "02.05.2021", 55),
                            new TargetHistoricalOccupation("05.05.21", "02.05.21", "02.05.2021", 30),
                            new TargetHistoricalOccupation("06.05.21", "02.05.21", "02.05.2021", 20)
                        }
                    },
                    new HistoricTestCase.TargetDayStats("05.05.21")
                    {
                        SourceData =
                        {
                            new SourceOccupation("01.05.21", "05.05.21", "01.01.2200", 80),
                            new SourceOccupation("02.05.21", "05.05.21", "01.01.2200", 60),
                            new SourceOccupation("03.05.21", "05.05.21", "01.01.2200", 65),
                            new SourceOccupation("04.05.21", "05.05.21", "01.01.2200", 80),
                            new SourceOccupation("05.05.21", "05.05.21", "05.05.2021", 65),
                            new SourceOccupation("06.05.21", "05.05.21", "05.05.2021", 25)
                        },
                        ExpectedData =
                        {
                            new TargetHistoricalOccupation("01.05.21", "01.05.21", "01.01.2200", 80),
                            new TargetHistoricalOccupation("02.05.21", "01.05.21", "01.05.2021", 50),
                            new TargetHistoricalOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new TargetHistoricalOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new TargetHistoricalOccupation("05.05.21", "01.05.21", "01.05.2021", 20),
                            new TargetHistoricalOccupation("06.05.21", "01.05.21", "01.05.2021", 15),
                            
                            new TargetHistoricalOccupation("02.05.21", "02.05.21", "01.01.2200", 60),
                            new TargetHistoricalOccupation("03.05.21", "02.05.21", "04.05.2021", 60),
                            new TargetHistoricalOccupation("04.05.21", "02.05.21", "04.05.2021", 55),
                            new TargetHistoricalOccupation("05.05.21", "02.05.21", "04.05.2021", 30),
                            new TargetHistoricalOccupation("06.05.21", "02.05.21", "04.05.2021", 20),
                            
                            new TargetHistoricalOccupation("03.05.21", "05.05.21", "01.01.2200", 60),
                            new TargetHistoricalOccupation("04.05.21", "05.05.21", "01.01.2200", 55),
                            new TargetHistoricalOccupation("05.05.21", "05.05.21", "05.05.2021", 30),
                            new TargetHistoricalOccupation("06.05.21", "05.05.21", "05.05.2021", 20)
                        }
                    }
                }
            };

            await RunTestCase(testCase);
        }

        private async Task RunTestCase(HistoricTestCase testCase)
        {
            var source = new SqlDataPumperSourceTarget();
            await source.Initialize("Server=(local);Database=DataPumper.Test.Source;Integrated Security=True");

            var target = new SqlDataPumperSourceTarget();
            await target.Initialize("Server=(local);Database=DataPumper.Test.Target;Integrated Security=True");

            DateTime? lastPumpDate = null;
            foreach (var pump in testCase.Pumps)
            {
                // Готовим исходные данные

                _sourceContext.Occupations.RemoveRange(_sourceContext.Occupations.ToList());
                await _sourceContext.SaveChangesAsync();
                foreach (var d in pump.SourceData)
                {
                    await _sourceContext.Occupations.AddAsync(d);
                    await _sourceContext.SaveChangesAsync();
                }
                
                await _sourceContext.SaveChangesAsync();

                // Осуществляем переливку
                await _pumper.Pump(source, target, new PumpParameters(
                    new DataSource("Occupations"),
                    new DataSource("Occupations"),
                    "ActualityDate",
                    lastPumpDate?.AddDays(1),
                    pump.Date));

                lastPumpDate = pump.Date;

                // Проверяем результат
                var factTarget = await _targetContext.Occupations.AsNoTracking().ToListAsync();
                factTarget.Count.Should().Be(pump.ExpectedData.Count);
                foreach (var pair in factTarget.Zip(pump.ExpectedData))
                {
                    pair.First.Should().Be(pair.Second, $"Error processing for date: {pump.Date}");
                }
            }
        }
    }

    public class HistoricTestCase
    {
        public List<TargetDayStats> Pumps { get; } = new();

        public class TargetDayStats
        {
            public DateTime Date { get; }
            
            public List<SourceOccupation> SourceData { get; } = new();
            public List<TargetHistoricalOccupation> ExpectedData { get; } = new();

            public TargetDayStats(string date)
            {
                Date = DateTime.ParseExact(date, "dd.MM.yy", CultureInfo.InvariantCulture);
            }
        }
    }
}