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
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlDataPumperSourceTarget));

        private SqlConnection _connection;
        private const int Timeout = 60 * 60 * 3; // 3 hours

        private const string Name = "Microsoft SQL Server";

        public event EventHandler<ProgressEventArgs> Progress;

        public string GetName()
        {
            return Name;
        }

        public async Task Initialize(string connectionString)
        {
            _connection?.Dispose();
            _connection = new SqlConnection(connectionString);
            await _connection.OpenAsync();
        }

        public Task<DateTime?> GetCurrentDate(string query)
        {
            return _connection.ExecuteScalarAsync<DateTime?>(query, commandTimeout: Timeout);
        }

        public async Task<IDataReader> GetDataReader(TableName tableName, string actualityFieldName, DateTime? notOlderThan, string tenantField, string[] tenantCodes)
        {
            var handler = Progress;
            handler?.Invoke(this, new ProgressEventArgs(0, $"Selecting data from source table '{tableName}' ...", tableName));

            var inStatement = GetInStatement(tenantCodes);

            if (notOlderThan != null)
            {
                return await _connection.ExecuteReaderAsync(
                    $"SELECT * FROM {tableName} WHERE {actualityFieldName} >= @NotOlderThan" + GetQuerySuffix(tenantField, tenantCodes, inStatement, "AND"), 
                    new
                    {
                        NotOlderThan = notOlderThan
                    }, commandTimeout: Timeout);
            }

            return await _connection.ExecuteReaderAsync(
                $"SELECT * FROM {tableName}" + GetQuerySuffix(tenantField, tenantCodes, inStatement, "WHERE"), commandTimeout: Timeout);
        }

        public async Task CleanupTable(CleanupTableRequest request)
        {
            var inStatement = GetInStatement(request.InstanceFieldValues);

            Log.Warn($"Cleaning target table '{request.TableName}', instances: ({inStatement}), actuality date >= {request.NotOlderThan}");
            int deleted;
            if (request.NotOlderThan == null || request.FullReloading)
            {
                deleted = await _connection.ExecuteAsync(
                    $"DELETE FROM {request.TableName}" + GetQuerySuffix(request.InstanceFieldName, request.InstanceFieldValues, inStatement, "WHERE"), commandTimeout: Timeout);
            }
            else
            {
                deleted = await _connection.ExecuteAsync(
                    $"DELETE FROM {request.TableName} WHERE {request.ActualityFieldName} >= @NotOlderThan" + GetQuerySuffix(request.InstanceFieldName, request.InstanceFieldValues, inStatement, "AND"),
                    new
                    {
                        NotOlderThan = request.NotOlderThan.Value
                    }, commandTimeout: Timeout);
            }
            Log.Warn($"Deleted {deleted} record(s) in target table '{request.TableName}'");
        }

        public async Task CleanupHistoryTable(CleanupTableRequest request)
        {
            var inStatement = GetInStatement(request.InstanceFieldValues);

            Log.Warn($"Cleaning target table '{request.TableName}' in history mode, instances: ({inStatement}), history date from = {request.CurrentPropertyDate}");

            var deleted = 0;
            if (request.FullReloading)
            {
                deleted = await _connection.ExecuteAsync(
                    $"DELETE FROM {request.TableName}" + GetQuerySuffix(request.InstanceFieldName, request.InstanceFieldValues, inStatement, "WHERE"), commandTimeout: Timeout);
            }
            else
            {
                deleted = await _connection.ExecuteAsync(
                    $"DELETE FROM {request.TableName} WHERE {request.HistoryDateFromFieldName} = @CurrentPropertyDate" + GetQuerySuffix(request.InstanceFieldName, request.InstanceFieldValues, inStatement, "AND"),
                    new
                    {
                        request.CurrentPropertyDate
                    }, commandTimeout: Timeout);
            }
            Log.Warn($"Deleted {deleted} record(s) in target table '{request.TableName}'");
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
                BulkCopyTimeout = Timeout,
                NotifyAfter = 10000,
                DestinationTableName = tableName.ToString()
            })
            {
                for (var i = 0; i < dataReader.FieldCount; i++)
                {
                    var column = dataReader.GetName(i);
                    bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column, column));
                }

                bulkCopy.SqlRowsCopied += (sender, args) =>
                {
                    Log.Info($"Records processed: {args.RowsCopied:#########}, table name {tableName}");
                    var handler = Progress;
                    handler?.Invoke(this, new ProgressEventArgs(args.RowsCopied, "Copying data...", tableName, sw.Elapsed));
                };

                await bulkCopy.WriteToServerAsync(dataReader);

                processed = bulkCopy.GetRowsCopied();
            }

            return processed;
        }

        private async Task CheckTablesCompatibility(TableName tableName, IDataReader dataReader)
        {
            using (var targetReader = await _connection.ExecuteReaderAsync($"SELECT TOP 0 * FROM {tableName}", commandTimeout: Timeout))
            {
                var sourceFields = Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToList();
                var targetFields = new HashSet<string>(Enumerable.Range(0, targetReader.FieldCount).Select(targetReader.GetName));

                var nonMatchingFields = sourceFields.Where(sf => !targetFields.Contains(sf)).ToList();
                if (nonMatchingFields.Any())
                    throw new ApplicationException($"Source table contains fields that not present in target table: {string.Join(",", nonMatchingFields)}");
            }
        }

        public async Task RunQuery(string queryText)
        {
            if (!string.IsNullOrEmpty(queryText))
            {
                var sw = new Stopwatch();
                sw.Start();
                Log.Info($"Running query: '{queryText}'");
                await _connection.ExecuteAsync(queryText, commandTimeout: Timeout);
                Log.Info($"Query finished in {sw.Elapsed}");
            }
        }

        private static string GetInStatement(string[] instanceFieldValues)
        {
            return instanceFieldValues != null
                ? string.Join(",", instanceFieldValues.Select(v => $"'{v}'").ToArray())
                : string.Empty;
        }

        private static string GetQuerySuffix(string tenantField, string[] tenantCodes, string inStatement, string command)
        {
            var querySuffix = string.Empty;
            if (tenantCodes != null && tenantCodes.Any() && tenantField != null)
                querySuffix = $" {command} {tenantField} IN ({inStatement})";
            return querySuffix;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _connection?.Dispose();                    
            }
            _disposed = true;
        }
    }
}