using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperSource : IDataPumperProvider
    {
        Task<DateTime?> GetCurrentDate(string query);
        
        Task<IDataReader> GetDataReader(TableName tableName, string fieldName, DateTime? notOlderThan, string tenantField, string[] tenantCodes);
    }
}