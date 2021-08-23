using System;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperSource : IDataPumperProvider
    {
        Task<DateTime?> GetCurrentDate(string query);
        
        Task<IDataReader> GetDataReader(DataReaderRequest request);
    }
}