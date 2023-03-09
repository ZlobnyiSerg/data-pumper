using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DataPumper.Core;
using DataPumper.Sql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NDataPumper = DataPumper.Core.HistoricDataPumper;
using Xunit;
using Xunit.Abstractions;

namespace DataPumper.Tests
{
    [CollectionDefinition("Pumping tests", DisableParallelization = true)]
    public class TestHistoricalPumping
    {
        private const string ClosedDate = "01.01.2200";
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
        public async Task TestSimplePumping()
        {
            // Arrange
            var source = new SqlDataPumperSourceTarget();
            await source.Initialize("Server=(local);Database=DataPumper.Test.Source;Integrated Security=True");

            var target = new SqlDataPumperSourceTarget();
            await target.Initialize("Server=(local);Database=DataPumper.Test.Target;Integrated Security=True");

            _sourceContext.Occupations.Add(new SourceOccupation("01.01.01", "01.01.01", "01.01.2001", 50));
            await _sourceContext.SaveChangesAsync();

            // Act
            var result = await new NDataPumper().Pump(source, target,
                new PumpParameters(
                    new DataSource("Occupations"),
                    new DataSource("Occupations"),
                    "ActualityDate",
                    new DateTime(2001, 01, 01),
                    new DateTime(2001, 01, 01),
                    nameof(TargetHistoricalOccupation.HistoryDateFrom),
                    nameof(TargetHistoricalOccupation.HistoryDateTo)));

            // Assert
            result.Inserted.Should().Be(1);
            result.Deleted.Should().Be(0);
            _targetContext.Occupations.Should().HaveCount(1);
        }

        [Fact(DisplayName = "Переливка дважды в тот же день обновляет данные")]
        public async Task TestSameDayPumping()
        {
            // Arrange
            var source = new SqlDataPumperSourceTarget();
            await source.Initialize("Server=(local);Database=DataPumper.Test.Source;Integrated Security=True");

            var target = new SqlDataPumperSourceTarget();
            await target.Initialize("Server=(local);Database=DataPumper.Test.Target;Integrated Security=True");

            _sourceContext.Occupations.Add(new SourceOccupation("01.01.01", "01.01.01", "01.01.2001", 50));
            await _sourceContext.SaveChangesAsync();

            // Act
            var result = await new NDataPumper().Pump(source, target,
                new PumpParameters(
                    new DataSource("Occupations"),
                    new DataSource("Occupations"),
                    "ActualityDate",
                    new DateTime(2001, 01, 01),
                    new DateTime(2001, 01, 01),
                    nameof(TargetHistoricalOccupation.HistoryDateFrom),
                    nameof(TargetHistoricalOccupation.HistoryDateTo)));
            // Pump again
            result = await new NDataPumper().Pump(source, target,
                new PumpParameters(
                    new DataSource("Occupations"),
                    new DataSource("Occupations"),
                    "ActualityDate",
                    new DateTime(2001, 01, 01),
                    new DateTime(2001, 01, 01),
                    nameof(TargetHistoricalOccupation.HistoryDateFrom),
                    nameof(TargetHistoricalOccupation.HistoryDateTo)));

            // Assert
            result.Inserted.Should().Be(1);
            result.Deleted.Should().Be(1);
            _targetContext.Occupations.Should().HaveCount(1);
        }

        [Fact(DisplayName = "Проверка загрузки исторических данных с пропущенными днями")]
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
                            new SourceOccupation("01.05.21", "02.05.21", ClosedDate, 83),
                            new SourceOccupation("02.05.21", "02.05.21", "02.05.2021", 60),
                            new SourceOccupation("03.05.21", "02.05.21", "02.05.2021", 60),
                            new SourceOccupation("04.05.21", "02.05.21", "02.05.2021", 55),
                            new SourceOccupation("05.05.21", "02.05.21", "02.05.2021", 30),
                            new SourceOccupation("06.05.21", "02.05.21", "02.05.2021", 20)
                        },
                        ExpectedData =
                        {
                            new TargetHistoricalOccupation("01.05.21", "01.05.21", "01.05.2021", 80),
                            new TargetHistoricalOccupation("02.05.21", "01.05.21", "01.05.2021", 50),
                            new TargetHistoricalOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new TargetHistoricalOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new TargetHistoricalOccupation("05.05.21", "01.05.21", "01.05.2021", 20),
                            new TargetHistoricalOccupation("06.05.21", "01.05.21", "01.05.2021", 15),
                            
                            new TargetHistoricalOccupation("01.05.21", "02.05.21", ClosedDate, 83),
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
                            new SourceOccupation("01.05.21", "05.05.21", ClosedDate, 83),
                            new SourceOccupation("02.05.21", "05.05.21", ClosedDate, 60),
                            new SourceOccupation("03.05.21", "05.05.21", ClosedDate, 65),
                            new SourceOccupation("04.05.21", "05.05.21", ClosedDate, 80),
                            new SourceOccupation("05.05.21", "05.05.21", "05.05.2021", 65),
                            new SourceOccupation("06.05.21", "05.05.21", "05.05.2021", 25)
                        },
                        ExpectedData =
                        {
                            new TargetHistoricalOccupation("01.05.21", "01.05.21", "01.05.2021", 80),
                            new TargetHistoricalOccupation("02.05.21", "01.05.21", "01.05.2021", 50),
                            new TargetHistoricalOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new TargetHistoricalOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new TargetHistoricalOccupation("05.05.21", "01.05.21", "01.05.2021", 20),
                            new TargetHistoricalOccupation("06.05.21", "01.05.21", "01.05.2021", 15),
                            
                            new TargetHistoricalOccupation("01.05.21", "02.05.21", ClosedDate, 83),
                            new TargetHistoricalOccupation("02.05.21", "02.05.21", "04.05.2021", 60),
                            new TargetHistoricalOccupation("03.05.21", "02.05.21", "04.05.2021", 60),
                            new TargetHistoricalOccupation("04.05.21", "02.05.21", "04.05.2021", 55),
                            new TargetHistoricalOccupation("05.05.21", "02.05.21", "04.05.2021", 30),
                            new TargetHistoricalOccupation("06.05.21", "02.05.21", "04.05.2021", 20),
                            
                            new TargetHistoricalOccupation("02.05.21", "05.05.21", ClosedDate, 60),
                            new TargetHistoricalOccupation("03.05.21", "05.05.21", ClosedDate, 65),
                            new TargetHistoricalOccupation("04.05.21", "05.05.21", ClosedDate, 80),
                            new TargetHistoricalOccupation("05.05.21", "05.05.21", "05.05.2021", 65),
                            new TargetHistoricalOccupation("06.05.21", "05.05.21", "05.05.2021", 25)
                        }
                    },
                    new HistoricTestCase.TargetDayStats("05.05.21")
                    {
                        SourceData =
                        {
                            new SourceOccupation("01.05.21", "05.05.21", ClosedDate, 83),
                            new SourceOccupation("02.05.21", "05.05.21", ClosedDate, 60),
                            new SourceOccupation("03.05.21", "05.05.21", ClosedDate, 65),
                            new SourceOccupation("04.05.21", "05.05.21", ClosedDate, 80),
                            new SourceOccupation("05.05.21", "05.05.21", "05.05.2021", 67),
                            new SourceOccupation("06.05.21", "05.05.21", "05.05.2021", 27)
                        },
                        ExpectedData =
                        {
                            new TargetHistoricalOccupation("01.05.21", "01.05.21", "01.05.2021", 80),
                            new TargetHistoricalOccupation("02.05.21", "01.05.21", "01.05.2021", 50),
                            new TargetHistoricalOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new TargetHistoricalOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new TargetHistoricalOccupation("05.05.21", "01.05.21", "01.05.2021", 20),
                            new TargetHistoricalOccupation("06.05.21", "01.05.21", "01.05.2021", 15),
                            
                            new TargetHistoricalOccupation("01.05.21", "02.05.21", ClosedDate, 83),
                            new TargetHistoricalOccupation("02.05.21", "02.05.21", "04.05.2021", 60),
                            new TargetHistoricalOccupation("03.05.21", "02.05.21", "04.05.2021", 60),
                            new TargetHistoricalOccupation("04.05.21", "02.05.21", "04.05.2021", 55),
                            new TargetHistoricalOccupation("05.05.21", "02.05.21", "04.05.2021", 30),
                            new TargetHistoricalOccupation("06.05.21", "02.05.21", "04.05.2021", 20),
                            
                            new TargetHistoricalOccupation("02.05.21", "05.05.21", ClosedDate, 60),
                            new TargetHistoricalOccupation("03.05.21", "05.05.21", ClosedDate, 65),
                            new TargetHistoricalOccupation("04.05.21", "05.05.21", ClosedDate, 80),
                            new TargetHistoricalOccupation("05.05.21", "05.05.21", "05.05.2021", 67),
                            new TargetHistoricalOccupation("06.05.21", "05.05.21", "05.05.2021", 27)
                        }
                    }
                }
            };

            await RunTestCase(testCase);
        }
        
        [Fact(DisplayName = "Проверка повторной загрузки дня в историческом режиме")]
        public async Task TestHistoricalReload()
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
                    new HistoricTestCase.TargetDayStats("01.05.21")
                    {
                        SourceData =
                        {
                            new SourceOccupation("01.05.21", "01.05.21", "01.05.2021", 85),
                            new SourceOccupation("02.05.21", "01.05.21", "01.05.2021", 51),
                            new SourceOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new SourceOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new SourceOccupation("05.05.21", "01.05.21", "01.05.2021", 23),
                            new SourceOccupation("06.05.21", "01.05.21", "01.05.2021", 15)
                        },
                        ExpectedData =
                        {
                            new TargetHistoricalOccupation("01.05.21", "01.05.21", "01.05.2021", 85),
                            new TargetHistoricalOccupation("02.05.21", "01.05.21", "01.05.2021", 51),
                            new TargetHistoricalOccupation("03.05.21", "01.05.21", "01.05.2021", 45),
                            new TargetHistoricalOccupation("04.05.21", "01.05.21", "01.05.2021", 30),
                            new TargetHistoricalOccupation("05.05.21", "01.05.21", "01.05.2021", 23),
                            new TargetHistoricalOccupation("06.05.21", "01.05.21", "01.05.2021", 15)
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
                    lastPumpDate,
                    pump.PropertyDate,
                    nameof(TargetHistoricalOccupation.HistoryDateFrom),
                    nameof(TargetHistoricalOccupation.HistoryDateTo)));

                lastPumpDate = pump.PropertyDate;

                // Проверяем результат
                var factTarget = await _targetContext.Occupations.AsNoTracking().ToListAsync();
                factTarget.Count.Should().Be(pump.ExpectedData.Count);
                foreach (var (fact, expected) in factTarget.Zip(pump.ExpectedData))
                {
                    fact.Should().Be(expected, $"Error processing for date: {pump.PropertyDate}");
                }
            }
        }
    }

    public class HistoricTestCase
    {
        public List<TargetDayStats> Pumps { get; } = new();

        public class TargetDayStats
        {
            public DateTime PropertyDate { get; }
            
            public List<SourceOccupation> SourceData { get; } = new();
            public List<TargetHistoricalOccupation> ExpectedData { get; } = new();

            public TargetDayStats(string propertyDate)
            {
                PropertyDate = DateTime.ParseExact(propertyDate, "dd.MM.yy", CultureInfo.InvariantCulture);
            }
        }
    }
}