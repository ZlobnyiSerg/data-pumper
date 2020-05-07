using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DataPumper.Core;
using Microsoft.Extensions.Logging;

namespace DataPumper.Sql
{
    public class SqlDataPumperSourceTarget : IDataPumperSource, IDataPumperTarget, IDisposable
    {
        private readonly ILogger<SqlDataPumperSourceTarget> _logger;
        private SqlConnection _connection;
        private int _timeout = 60 * 60 * 3; // 3 hours

        public SqlDataPumperSourceTarget(ILogger<SqlDataPumperSourceTarget> logger)
        {
            _logger = logger;
        }

        public const string Name = "Microsoft SQL Server"; 

        public string GetName()
        {
            return Name;
        }

        public async Task Initialize(string connectionString)
        {
            if (_connection != null)
                await _connection.DisposeAsync();
            _connection = new SqlConnection(connectionString);
            await _connection.OpenAsync();
        }

        public async IAsyncEnumerable<TableDefinition> GetTables()
        {
            foreach (var table in await _connection.QueryAsync<dynamic>("SELECT * FROM INFORMATION_SCHEMA.TABLES"))
            {
                yield return new TableDefinition(new TableName(table.TABLE_SCHEMA, table.TABLE_NAME), null);
            }
        }

        public Task<DateTime?> GetCurrentDate(TableName tableName, string fieldName)
        {
            return _connection.ExecuteScalarAsync<DateTime?>($"SELECT Min({fieldName}) FROM {tableName}", commandTimeout: _timeout);
        }
        
        public async Task<string[]> GetInstances(TableName tableName, string fieldName)
        {
            var result = new List<string>();
            await using (var reader = await _connection.ExecuteReaderAsync($"SELECT {fieldName} FROM {tableName}", commandTimeout: _timeout))
            {
                while (await reader.ReadAsync())
                {
                    result.Add(reader.GetString(0));
                }
            }
            return result.ToArray();
        }

        public async Task<IDataReader> GetDataReader(TableName tableName, string fieldName, DateTime? notOlderThan)
        {
            var handler = Progress;
            handler?.Invoke(this, new ProgressEventArgs(0, $"Selecting data from source table '{tableName}' ...", null));
            if (notOlderThan != null)
            {
                return await _connection.ExecuteReaderAsync($"SELECT * FROM {tableName} WHERE {fieldName} >= @NotOlderThan", new
                {
                    NotOlderThan = notOlderThan
                }, commandTimeout: _timeout);
            }

            return await _connection.ExecuteReaderAsync($"SELECT * FROM {tableName}", commandTimeout: _timeout);
        }

        public Task CleanupTable(CleanupTableRequest request)
        {
            var handler = Progress;
            handler?.Invoke(this, new ProgressEventArgs(0, $"Cleaning target table..."));
            var inStatement = string.Join(',', $"'{request.InstanceFieldValues}'");
            if (request.NotOlderThan == null)
            {
                return _connection.ExecuteAsync($"TRUNCATE TABLE {request.TableName} WHERE {request.InstanceFieldName} IN ({inStatement})", commandTimeout: _timeout);
            }
            return _connection.ExecuteAsync($"DELETE FROM {request.TableName} WHERE {request.InstanceFieldName} IN ({inStatement}) AND {request.ActualityFieldName} >= @NotOlderThan", new
            {
                NotOlderThan = request.NotOlderThan.Value
            }, commandTimeout: _timeout);
        }

        public async Task<long> InsertData(TableName tableName, IDataReader dataReader)
        {
            await CheckTablesCompatibility(tableName, dataReader);
            var sw = new Stopwatch();
            sw.Start();
            using var bulkCopy = new SqlBulkCopy(_connection)
            {
                BatchSize = 1000000,
                BulkCopyTimeout = _timeout,
                NotifyAfter = 10000,
                DestinationTableName = tableName.ToString()
            };

            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                var column = dataReader.GetName(i);
                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column, column));
            }

            long processed = 0;

            bulkCopy.SqlRowsCopied += (sender, args) =>
            {
                _logger.LogInformation($"Records processed: {args.RowsCopied:#########}");
                var handler = Progress;
                handler?.Invoke(this, new ProgressEventArgs(args.RowsCopied, "Copying data...", sw.Elapsed));
                processed = args.RowsCopied;
            };

            await bulkCopy.WriteToServerAsync(dataReader);
            return processed;
        }

        private async Task CheckTablesCompatibility(TableName tableName, IDataReader dataReader)
        {
            await using var targetReader = await _connection.ExecuteReaderAsync($"SELECT TOP 0 * FROM {tableName}", commandTimeout: _timeout);

            var sourceFields = Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToList();
            var targetFields = Enumerable.Range(0, targetReader.FieldCount).Select(targetReader.GetName).ToHashSet();

            var nonMatchingFields = sourceFields.Where(sf => !targetFields.Contains(sf)).ToList();
            if (nonMatchingFields.Any())
                throw new ApplicationException($"Source table contains fields that not present in target table: "+string.Join(",", nonMatchingFields));
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}