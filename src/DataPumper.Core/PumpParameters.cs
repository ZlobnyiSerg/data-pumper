using System;

namespace DataPumper.Core
{
    public class PumpParameters
    {
        public DataSource SourceDataSource { get; private set; }
        public DataSource TargetDataSource { get; private set; }
        public string ActualityFieldName { get; private set; }
        public string? TenantField { get; private set; }
        public DateTime? OnDate { get; private set; }
        public DateTime CurrentDate { get; private set; }
        public bool FullReloading { get; private set; }
        public string[]? TenantCodes { get; private set; }
        
        public DateTime? DeleteProtectionDate { get; set; }
        
        public FilterConstraint[]? Filter { get; set; }

        public PumpParameters(
            DataSource sourceDataSource, 
            DataSource targetDataSource,
            string actualityFieldName,
            DateTime? onDate,
            DateTime currentDate,
            bool fullReloading = false,
            string? tenantField = null,
            string[]? tenantCodes = null)
        {
            SourceDataSource = sourceDataSource;
            TargetDataSource = targetDataSource;
            ActualityFieldName = actualityFieldName;
            TenantField = tenantField;
            OnDate = onDate;
            CurrentDate = currentDate;
            FullReloading = fullReloading;
            TenantCodes = tenantCodes;
        }
    }
}