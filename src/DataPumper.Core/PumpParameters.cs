using System;

namespace DataPumper.Core
{
    public class PumpParameters
    {
        public DataSource SourceDataSource { get; }
        public DataSource TargetDataSource { get; }
        public string ActualityFieldName { get; }
        public string? TenantField { get; }
        public string HistoricColumnsFrom { get; }
        public string HistoricColumnsTo { get; }
        public DateTime? OnDate { get; }
        public DateTime? LastLoadDate { get; }
        public DateTime CurrentDate { get; }
        public bool FullReloading { get; }
        public string[]? TenantCodes { get; }
        
        public DateTime? DeleteProtectionDate { get; set; }
        
        public FilterConstraint[]? Filter { get; set; }

        public PumpParameters(
            DataSource sourceDataSource, 
            DataSource targetDataSource,
            string actualityFieldName,
            DateTime? onDate,
            DateTime? lastLoadDate,
            DateTime currentDate,
            string historicColumnsFrom,
            string historicColumnsTo,
            bool fullReloading = false,
            string? tenantField = null,
            string[]? tenantCodes = null)
        {
            SourceDataSource = sourceDataSource;
            TargetDataSource = targetDataSource;
            ActualityFieldName = actualityFieldName;
            TenantField = tenantField;
            OnDate = onDate;
            LastLoadDate = lastLoadDate;
            CurrentDate = currentDate;
            HistoricColumnsFrom = historicColumnsFrom;
            HistoricColumnsTo = historicColumnsTo;
            FullReloading = fullReloading;
            TenantCodes = tenantCodes;
        }
    }
}