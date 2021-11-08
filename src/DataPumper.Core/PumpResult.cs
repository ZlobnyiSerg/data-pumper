namespace DataPumper.Core
{
    /// <summary>
    /// Результат перекачки данных
    /// </summary>
    public readonly struct PumpResult
    {
        /// <summary>
        /// Количество вставленных записей
        /// </summary>
        public long Inserted { get; }
        
        /// <summary>
        /// Количество удалённых записей
        /// </summary>
        public long Deleted { get; }

        public PumpResult(long inserted, long deleted)
        {
            Inserted = inserted;
            Deleted = deleted;
        }
    }
}