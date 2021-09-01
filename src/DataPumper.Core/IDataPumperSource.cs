using System;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperSource : IDataPumperProvider
    {
        Task<DateTime?> GetCurrentDate(string query);
        
        /// <summary>
        /// Возвращает источник для чтения данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<IDataReader> GetDataReader(DataReaderRequest request);

        /// <summary>
        /// Возващает все колонки, присутсвующие в таблице
        /// </summary>
        /// <param name="tableName">Название таблицы</param>
        /// <returns>Массив колонок</returns>
        Task<string[]> GetTableFields(TableName tableName);
    }
}