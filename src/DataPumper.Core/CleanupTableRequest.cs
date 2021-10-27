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
        public DateTime? NotOlderThan { get; }
        
        public string? TenantField { get; }

        public string[]? TenantCodes { get; }

        public DateTime CurrentPropertyDate { get; }

        public DateTime? DeleteProtectionDate { get; set; }
        
        public FilterConstraint[]? Filter { get; set; }

        public CleanupTableRequest(
            DataSource dataSource, 
            string actualityFieldName,
            DateTime? notOlderThan,
            DateTime currentPropertyDate,
            string? tenantField = null, 
            string[]? tenantCodes = null
        )
        {
            DataSource = dataSource;
            NotOlderThan = notOlderThan;
            ActualityFieldName = actualityFieldName;
            CurrentPropertyDate = currentPropertyDate;
            TenantField = tenantField;
            TenantCodes = tenantCodes;
        }
    }
}