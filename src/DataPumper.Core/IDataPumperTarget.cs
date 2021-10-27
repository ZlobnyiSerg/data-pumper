using System;
using System.Data;
using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperTarget : IDataPumperProvider
    {
        /// <summary>
        /// Осуществляет подготовку таблицы к заливке, удаляя прогнозные данные, либо очищает полностью, если в запросе указан флаг <see cref="request.FullReloading"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<long> CleanupTable(CleanupTableRequest request);
        
        /// <summary>
        /// Осуществляет подготовку исторической таблицы к заливке порции данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<long> CleanupHistoryTable(CleanupTableRequest request);

        /// <summary>
        /// Закрывает исторические записи (записи, которые гарантированно ушли в прошлое и не могут больше измениться)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<int> CloseHistoricPeriods(CleanupTableRequest request);

        /// <summary>
        /// Перекачивает данные из источника
        /// </summary>
        /// <param name="targetDataSource">Название целевого источника данных</param>
        /// <param name="sourceDataReader">Источник данных</param>
        /// <returns></returns>
        Task<long> InsertData(DataSource targetDataSource, IDataReader sourceDataReader);

        /// <summary>
        /// Выполнить сырой SQL-запрос
        /// </summary>
        /// <param name="queryText">Текст запроса</param>
        /// <returns></returns>
        Task ExecuteRawQuery(string queryText);

        /// <summary>
        /// Событие, вызывается при прогрессе длительных операций
        /// </summary>
        event EventHandler<ProgressEventArgs> Progress;
    }
}