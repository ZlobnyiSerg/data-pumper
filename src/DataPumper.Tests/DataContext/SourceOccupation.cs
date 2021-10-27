using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DataPumper.Tests
{
    public class SourceOccupation
    {
        [Key]
        public int Id { get; set; }

        public DateTime ActualityDate { get; set; }
        
        public DateTime HistoryDateFrom { get; set; }

        public DateTime HistoryDateTo { get; set; }

        public int OccPercent { get; set; }

        public SourceOccupation(string actualDate, string historyDateFrom, string historyDateTo, int occupationPercent)
        {
            ActualityDate = DateTime.ParseExact(actualDate, "dd.MM.yy", CultureInfo.InvariantCulture);
            HistoryDateFrom = DateTime.ParseExact(historyDateFrom, "dd.MM.yy", CultureInfo.InvariantCulture);
            HistoryDateTo = DateTime.ParseExact(historyDateTo, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            OccPercent = occupationPercent;
        }

        public SourceOccupation()
        {
        }
    }
}