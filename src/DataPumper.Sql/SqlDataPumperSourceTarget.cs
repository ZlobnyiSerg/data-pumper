using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using Dapper;
using DataPumper.Core;

namespace DataPumper.Sql
{
    public class SqlDataPumperSourceTarget : IDataPumperSource, IDataPumperTarget, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SqlDataPumperSourceTarget));

        private SqlConnection _connection;
        private int _timeout = 60 * 60 * 3; // 3 hours

        public SqlDataPumperSourceTarget()
        {
        }

        public const string Name = "Microsoft SQL Server"; 

        public string GetName()
        {
            return Name;
        }

        public async Task Initialize(string connectionString)
        {
            if (_connection != null)
                _connection.Dispose();
            _connection = new SqlConnection(connectionString);
            await _connection.OpenAsync();
        }

        public Task<DateTime?> GetCurrentDate(string query)
        {
            return _connection.ExecuteScalarAsync<DateTime?>(query, commandTimeout: _timeout);
        }
        
        public async Task<string[]> GetInstances(TableName tableName, string fieldName)
        {
            var result = new List<string>();
            using (var reader = await _connection.ExecuteReaderAsync($"SELECT {fieldName} FROM {tableName}", commandTimeout: _timeout))
            {
                while (reader.Read())
                {
                    result.Add(reader.GetString(0));
                }
            }
            return result.ToArray();
        }

        public async Task<IDataReader> GetDataReader(TableName tableName, string fieldName, DateTime? notOlderThan)
        {
            if (notOlderThan != null)
            {
                return await _connection.ExecuteReaderAsync($"SELECT * FROM {tableName} WHERE {fieldName} >= @NotOlderThan", new
                {
                    NotOlderThan = notOlderThan
                }, commandTimeout: _timeout);
            }

            return await _connection.ExecuteReaderAsync($"SELECT * FROM {tableName}", commandTimeout: _timeout);
        }

        public async Task CleanupTable(CleanupTableRequest request)
        {            
            var inStatement = string.Join(",", request.InstanceFieldValues.Select(v=>$"'{v}'").ToArray());
            _logger.Warn($"Cleaning target table, instances: ({inStatement}), actuality date >= {request.NotOlderThan}");
            int deleted;
            if (request.NotOlderThan == null)
            {
                deleted = await _connection.ExecuteAsync($"DELETE FROM {request.TableName} WHERE {request.InstanceFieldName} IN ({inStatement})", commandTimeout: _timeout);
            }
            else
            {
                deleted = await _connection.ExecuteAsync(
                    $"DELETE FROM {request.TableName} WHERE {request.InstanceFieldName} IN ({inStatement}) AND {request.ActualityFieldName} >= @NotOlderThan",
                    new
                    {
                        NotOlderThan = request.NotOlderThan.Value
                    }, commandTimeout: _timeout);
            }
            _logger.Warn($"Deleted {deleted} record(s)");
        }

        public async Task<long> InsertData(TableName tableName, IDataReader dataReader)
        {
            await CheckTablesCompatibility(tableName, dataReader);
            var sw = new Stopwatch();
            sw.Start();
            long processed = 0;
            using (var bulkCopy = new SqlBulkCopy(_connection)
            {
                BatchSize = 1000000,
                BulkCopyTimeout = _timeout,
                NotifyAfter = 10000,
                DestinationTableName = tableName.ToString()
            })
            {
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    var column = dataReader.GetName(i);
                    bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column, column));
                }

                await bulkCopy.WriteToServerAsync(dataReader);

                processed = bulkCopy.GetRowsCopied();
            }
            
            return processed;
        }

        private async Task CheckTablesCompatibility(TableName tableName, IDataReader dataReader)
        {
            using (var targetReader = await _connection.ExecuteReaderAsync($"SELECT TOP 0 * FROM {tableName}", commandTimeout: _timeout))
            {
                var sourceFields = Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToList();
                var targetFields = new HashSet<string>( Enumerable.Range(0, targetReader.FieldCount).Select(targetReader.GetName));

                var nonMatchingFields = sourceFields.Where(sf => !targetFields.Contains(sf)).ToList();
                if (nonMatchingFields.Any())
                    throw new ApplicationException($"Source table contains fields that not present in target table: " + string.Join(",", nonMatchingFields));
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}