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
        private int _timeout = 60 * 10;

        public SqlDataPumperSourceTarget(ILogger<SqlDataPumperSourceTarget> logger)
        {
            _logger = logger;
        }

        public string GetName()
        {
            return "Microsoft SQL Server";
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

        public async Task CleanupTable(TableName tableName, string fieldName, DateTime? notOlderThan)
        {
            var handler = Progress;
            handler?.Invoke(this, new ProgressEventArgs(0, $"Cleaning target table...", null));
            if (notOlderThan == null)
            {
                await _connection.ExecuteAsync($"TRUNCATE TABLE {tableName}", commandTimeout: _timeout);
            }
            else
            {
                await _connection.ExecuteAsync($"DELETE FROM {tableName} WHERE {fieldName} >= @NotOlderThan", new
                {
                    NotOlderThan = notOlderThan
                }, commandTimeout: _timeout);
            }
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
                NotifyAfter = 5000,
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