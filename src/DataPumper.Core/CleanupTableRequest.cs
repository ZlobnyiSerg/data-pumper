using System;

namespace DataPumper.Core
{
    /// <summary>
    /// Запрос на очистку целевой таблицы
    /// </summary>
    public class CleanupTableRequest
    {
        public DataSource DataSource { get; } 

        public string ActualityFieldName { get; } 

        /// <summary>
        /// Используется для частичтого обновления данных, начиная с указанной даты. Если параметр пуст, будет осуществлена полная переливка
        /// </summary>
        public DateTime? ActualityDateStart { get; }
        
        public string? TenantField { get; }

        public string[]? TenantCodes { get; }

        public string? HistoricColumnsFrom { get; }

        public string? HistoricColumnsTo { get; }

        public DateTime? LastLoadDate { get; }

        public DateTime CurrentPropertyDate { get; }

        public DateTime? DeleteProtectionDate { get; set; }
        
        public FilterConstraint[]? Filter { get; set; }

        public CleanupTableRequest(
            DataSource dataSource, 
            string actualityFieldName,
            DateTime? actualityDateStart,
            DateTime currentPropertyDate,
            string? tenantField = null, 
            string[]? tenantCodes = null,
            string? historicColumnsFrom = null,
            string? historicColumnsTo = null,
            DateTime? lastLoadDate = null
        )
        {
            DataSource = dataSource;
            ActualityDateStart = actualityDateStart;
            ActualityFieldName = actualityFieldName;
            CurrentPropertyDate = currentPropertyDate;
            TenantField = tenantField;
            TenantCodes = tenantCodes;
            HistoricColumnsFrom = historicColumnsFrom;
            HistoricColumnsTo = historicColumnsTo;
            LastLoadDate = lastLoadDate;
        }
    }
}