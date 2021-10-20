using System;

namespace DataPumper.Core
{
    public class PumpParameters
    {
        public DataSource SourceDataSource { get; private set; }
        public DataSource TargetDataSource { get; private set; }
        public string ActualityFieldName { get; private set; }
        public string HistoryDateFromFieldName { get; private set; }
        public string TenantField { get; private set; }
        public DateTime OnDate { get; private set; }
        public bool HistoricMode { get; private set; }
        public DateTime CurrentDate { get; private set; }
        public bool FullReloading { get; private set; }
        public string[] TenantCodes { get; private set; }
        
        public DateTime? DeleteProtectionDate { get; set; }
        
        public FilterConstraint[] Filter { get; set; }

        public PumpParameters(DataSource sourceDataSource, DataSource targetDataSource, string actualityFieldName, string historyDateFromFieldName, DateTime onDate, DateTime currentDate)
        {
            SourceDataSource = sourceDataSource;
            TargetDataSource = targetDataSource;
            ActualityFieldName = actualityFieldName;
            HistoryDateFromFieldName = historyDateFromFieldName;
            OnDate = onDate;
            CurrentDate = currentDate;
        }

        public PumpParameters(DataSource sourceDataSource, DataSource targetDataSource, string actualityFieldName, string historyDateFromFieldName, string tenantField, DateTime onDate, DateTime currentDate, string[] tenantCodes)
        {
            SourceDataSource = sourceDataSource;
            TargetDataSource = targetDataSource;
            ActualityFieldName = actualityFieldName;
            HistoricMode = true;
            HistoryDateFromFieldName = historyDateFromFieldName;
            TenantField = tenantField;
            OnDate = onDate;
            CurrentDate = currentDate;
            TenantCodes = tenantCodes;
        }

        public PumpParameters(DataSource sourceDataSource, DataSource targetDataSource, string actualityFieldName, string historyDateFromFieldName, string tenantField, DateTime onDate, bool historicMode, DateTime currentDate, bool fullReloading, string[] tenantCodes)
        {
            SourceDataSource = sourceDataSource;
            TargetDataSource = targetDataSource;
            ActualityFieldName = actualityFieldName;
            HistoryDateFromFieldName = historyDateFromFieldName;
            TenantField = tenantField;
            OnDate = onDate;
            HistoricMode = historicMode;
            CurrentDate = currentDate;
            FullReloading = fullReloading;
            TenantCodes = tenantCodes;
        }
    }
}