using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperTarget : IDataPumperProvider
    {
        Task CleanupTable(TableName tableName, string fieldName, DateTime? notOlderThan);

        Task<long> InsertData(TableName tableName, IDataReader dataReader);

        event EventHandler<ProgressEventArgs> Progress;
    }
}