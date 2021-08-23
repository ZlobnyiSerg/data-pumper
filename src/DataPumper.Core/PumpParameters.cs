using System;

namespace DataPumper.Core
{
    public class PumpParameters
    {
        public TableName SourceTable { get; private set; }
        public TableName TargetTable { get; private set; }
        public string ActualityFieldName { get; private set; }
        public string HistoryDateFromFieldName { get; private set; }
        public string TenantField { get; private set; }
        public DateTime OnDate { get; private set; }
        public bool HistoricMode { get; private set; }
        public DateTime CurrentDate { get; private set; }
        public bool FullReloading { get; private set; }
        public string[] TenantCodes { get; private set; }
        
        public DateTime? DeleteProtectionDate { get; set; }
        
        public FilterConstraint Filter { get; set; }

        public PumpParameters(TableName sourceTable, TableName targetTable, string actualityFieldName, string historyDateFromFieldName, DateTime onDate, DateTime currentDate)
        {
            SourceTable = sourceTable;
            TargetTable = targetTable;
            ActualityFieldName = actualityFieldName;
            HistoryDateFromFieldName = historyDateFromFieldName;
            OnDate = onDate;
            CurrentDate = currentDate;
        }

        public PumpParameters(TableName sourceTable, TableName targetTable, string actualityFieldName, string historyDateFromFieldName, string tenantField, DateTime onDate, DateTime currentDate, string[] tenantCodes)
        {
            SourceTable = sourceTable;
            TargetTable = targetTable;
            ActualityFieldName = actualityFieldName;
            HistoricMode = true;
            HistoryDateFromFieldName = historyDateFromFieldName;
            TenantField = tenantField;
            OnDate = onDate;
            CurrentDate = currentDate;
            TenantCodes = tenantCodes;
        }

        public PumpParameters(TableName sourceTable, TableName targetTable, string actualityFieldName, string historyDateFromFieldName, string tenantField, DateTime onDate, bool historicMode, DateTime currentDate, bool fullReloading, string[] tenantCodes)
        {
            SourceTable = sourceTable;
            TargetTable = targetTable;
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