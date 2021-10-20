using System;

namespace DataPumper.Core
{
    public class DataReaderRequest
    {
        public DataSource DataSource { get; }
        public string ActualityDateFieldName { get; }
        public DateTime? NotOlderThan { get; set; }
        public string TenantField { get; set; }
        public string[] TenantCodes { get; set; }
        
        public FilterConstraint[] Filter { get; set; }

        public DataReaderRequest(DataSource dataSource, string actualityDateFieldName)
        {
            DataSource = dataSource;
            ActualityDateFieldName = actualityDateFieldName;
        }
    }
}