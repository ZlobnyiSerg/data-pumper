using System;

namespace DataPumper.Core
{
    public class DataReaderRequest
    {
        public TableName TableName { get; }
        public string ActualityDateFieldName { get; }
        public DateTime? NotOlderThan { get; set; }
        public string TenantField { get; set; }
        public string[] TenantCodes { get; set; }
        
        public FilterConstraint Filter { get; set; }

        public DataReaderRequest(TableName tableName, string actualityDateFieldName)
        {
            TableName = tableName;
            ActualityDateFieldName = actualityDateFieldName;
        }
    }
}