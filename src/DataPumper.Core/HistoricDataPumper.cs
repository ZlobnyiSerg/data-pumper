using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using Common.Logging;

namespace DataPumper.Core
{
    public class HistoricDataPumper : IDataPumper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumper));
        
        public async Task<PumpResult> Pump(IDataPumperSource source, IDataPumperTarget target, PumpParameters parameters)
        {
            try
            {
                using var transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
                var sw = new Stopwatch();
                sw.Start();

                Log.Info(
                    $"Cleaning target table '{parameters.TargetDataSource}' (from date {parameters.OnDate}) for instances: {string.Join(",", parameters.TenantCodes ?? new string[0])}...");
                var cleanupTableRequest = new CleanupTableRequest(
                    parameters.TargetDataSource,
                    parameters.ActualityFieldName,
                    parameters.OnDate,
                    parameters.CurrentDate,
                    parameters.TenantField,
                    parameters.TenantCodes
                )
                {
                    DeleteProtectionDate = parameters.DeleteProtectionDate,
                    Filter = parameters.Filter
                };
                var deleted = await target.CleanupHistoryTable(cleanupTableRequest);
            
                Log.Info($"Cleaning '{parameters.TargetDataSource}' completed in {sw.Elapsed}, records deleted: {deleted}, transferring data...");
                sw.Restart();

                using var reader = await source.GetDataReader(
                    new DataReaderRequest(parameters.SourceDataSource, parameters.ActualityFieldName)
                    {
                        NotOlderThan = parameters.OnDate,
                        TenantField = parameters.TenantField,
                        TenantCodes = parameters.TenantCodes,
                        Filter = parameters.Filter
                    });
                var inserted = await target.InsertData(parameters.TargetDataSource, reader);
                Log.Info($"Data transfer '{parameters.TargetDataSource}' of {inserted} record(s) completed in {sw.Elapsed}");

                var updated = await target.CloseHistoricPeriods(cleanupTableRequest);
                Log.Info($"Updated {updated} historic record(s)");

                transaction.Complete();
                return new PumpResult(inserted, deleted);
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing {parameters.SourceDataSource} -> {parameters.TargetDataSource}", ex);
                throw;
            }
        }
        
        
    }

    public class HistoricDataReaderWrapper : IDataReader
    {
        private readonly IDataReader _dataReader;

        public HistoricDataReaderWrapper(IDataReader dataReader)
        {
            _dataReader = dataReader;
        }

        public void Dispose()
        {
            _dataReader.Dispose();
        }

        public string GetName(int i)
        {
            return _dataReader.GetName(i);
        }

        public string GetDataTypeName(int i)
        {
            return _dataReader.GetDataTypeName(i);
        }

        public Type GetFieldType(int i)
        {
            return _dataReader.GetFieldType(i);
        }

        public object GetValue(int i)
        {
            return _dataReader.GetValue(i);
        }

        public int GetValues(object[] values)
        {
            return _dataReader.GetValues(values);
        }

        public int GetOrdinal(string name)
        {
            return _dataReader.GetOrdinal(name);
        }

        public bool GetBoolean(int i)
        {
            return _dataReader.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            return _dataReader.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return _dataReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return _dataReader.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _dataReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public Guid GetGuid(int i)
        {
            return _dataReader.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            return _dataReader.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            return _dataReader.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            return _dataReader.GetInt64(i);
        }

        public float GetFloat(int i)
        {
            return _dataReader.GetFloat(i);
        }

        public double GetDouble(int i)
        {
            return _dataReader.GetDouble(i);
        }

        public string GetString(int i)
        {
            return _dataReader.GetString(i);
        }

        public decimal GetDecimal(int i)
        {
            return _dataReader.GetDecimal(i);
        }

        public DateTime GetDateTime(int i)
        {
            return _dataReader.GetDateTime(i);
        }

        public IDataReader GetData(int i)
        {
            return _dataReader.GetData(i);
        }

        public bool IsDBNull(int i)
        {
            return _dataReader.IsDBNull(i);
        }

        public int FieldCount => _dataReader.FieldCount;

        public object this[int i] => _dataReader[i];

        public object this[string name] => _dataReader[name];

        public void Close()
        {
            _dataReader.Close();
        }

        public DataTable GetSchemaTable()
        {
            return _dataReader.GetSchemaTable();
        }

        public bool NextResult()
        {
            return _dataReader.NextResult();
        }

        public bool Read()
        {
            return _dataReader.Read();
        }

        public int Depth => _dataReader.Depth;
        public bool IsClosed => _dataReader.IsClosed;
        public int RecordsAffected => _dataReader.RecordsAffected;
    }
}