using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
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
                    $"SELECT * FROM {tableName} WHERE {actualityFieldName} >= @NotOlderThan AND {GetTenantFilter(tenantField, tenantCodes, inStatement)}", 
                    new
                    {
                        NotOlderThan = notOlderThan
                    }, commandTimeout: Timeout);
            }

            return await _connection.ExecuteReaderAsync(
                $"SELECT * FROM {tableName} WHERE {GetTenantFilter(tenantField, tenantCodes, inStatement)}", commandTimeout: Timeout);
        }

        public async Task CleanupTable(CleanupTableRequest request)
        {
            var inStatement = GetInStatement(request.InstanceFieldValues);

            Log.Warn($"Cleaning target table '{request.TableName}'");
            int deleted;
            if (request.NotOlderThan == null || request.FullReloading)
            {
                var query = $@"DELETE FROM {request.TableName} WHERE 
                        ({GetTenantFilter(request.InstanceFieldName, request.InstanceFieldValues, inStatement)})
                        AND ({GetDeleteProtectionDateFilter(request)})";
                Log.Warn(query);
                deleted = await _connection.ExecuteAsync(query, commandTimeout: Timeout);
            }
            else
            {
                var query = $@"DELETE FROM {request.TableName} WHERE
                         {request.ActualityFieldName} >= @NotOlderThan 
                         AND ({GetTenantFilter(request.InstanceFieldName, request.InstanceFieldValues, inStatement)})
                         AND ({GetDeleteProtectionDateFilter(request)})";
                Log.Warn(query);
                deleted = await _connection.ExecuteAsync(
                    query,
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

            Log.Warn($"Cleaning target table '{request.TableName}' in history mode");

            var deleted = 0;
            if (request.FullReloading)
            {
                var query = $@"DELETE FROM {request.TableName} WHERE 
                        ({GetTenantFilter(request.InstanceFieldName, request.InstanceFieldValues, inStatement)})
                        AND ({GetDeleteProtectionDateFilter(request)})";
                Log.Warn(query);
                deleted = await _connection.ExecuteAsync(query, commandTimeout: Timeout);
            }
            else
            {
                var query = $@"DELETE FROM {request.TableName} WHERE 
                        {request.HistoryDateFromFieldName} = @CurrentPropertyDate
                        AND ({GetTenantFilter(request.InstanceFieldName, request.InstanceFieldValues, inStatement)})
                        AND ({GetDeleteProtectionDateFilter(request)})";
                Log.Warn(query);
                deleted = await _connection.ExecuteAsync(
                    query,
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

        private static string GetTenantFilter(string tenantField, string[] tenantCodes, string inStatement)
        {
            if (tenantCodes != null && tenantCodes.Any() && tenantField != null)
                return $"{tenantField} IN ({inStatement})";
            return "1=1";
        }
        
        private static string GetDeleteProtectionDateFilter(CleanupTableRequest request)
        {
            if (request.DeleteProtectionDate != null)
                return
                    $"{request.ActualityFieldName} >= cast('{request.DeleteProtectionDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}' as date)";
            return "1=1";
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