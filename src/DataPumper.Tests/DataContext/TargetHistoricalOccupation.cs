using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DataPumper.Tests
{
    public class TargetHistoricalOccupation
    {
        [Key] public int Id { get; set; }

        public DateTime ActualityDate { get; set; }

        public DateTime HistoryDateFrom { get; set; }

        public DateTime HistoryDateTo { get; set; }

        public int OccPercent { get; set; }

        public TargetHistoricalOccupation(string actualDate, string historyDateFrom, string historyDateTo, int occupationPercent)
        {
            ActualityDate = DateTime.ParseExact(actualDate, "dd.MM.yy", CultureInfo.InvariantCulture);
            HistoryDateFrom = DateTime.ParseExact(historyDateFrom, "dd.MM.yy", CultureInfo.InvariantCulture);
            HistoryDateTo = DateTime.ParseExact(historyDateTo, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            OccPercent = occupationPercent;
        }

        public TargetHistoricalOccupation()
        {
        }

        protected bool Equals(TargetHistoricalOccupation other)
        {
            return ActualityDate.Equals(other.ActualityDate) && HistoryDateFrom.Equals(other.HistoryDateFrom) && HistoryDateTo.Equals(other.HistoryDateTo) && OccPercent == other.OccPercent;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TargetHistoricalOccupation)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActualityDate, HistoryDateFrom, HistoryDateTo, OccPercent);
        }

        public override string ToString()
        {
            return $"{ActualityDate:dd.MM.yy} {HistoryDateFrom:dd.MM.yy} {HistoryDateTo:dd.MM.yyyy} {OccPercent}";
        }
    }
}