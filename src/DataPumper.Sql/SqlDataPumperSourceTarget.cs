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

        public async Task<IDataReader> GetDataReader(DataReaderRequest request)
        {
            var handler = Progress;
            handler?.Invoke(this, new ProgressEventArgs(0, $"Selecting data from source table '{request.DataSource}' ...", request.DataSource));

            if (request.DataSource.IsStoredProcedure)
                return await GetStoredProcedureDataReader(request);

            var inStatement = GetInStatement(request.TenantCodes);

            if (request.NotOlderThan != null)
            {
                return await _connection.ExecuteReaderAsync(
                    $@"SELECT * FROM {request.DataSource} WHERE {request.ActualityDateFieldName} >= @NotOlderThan
                       AND ({GetFilterPredicate(request.Filter)}) 
                       AND {GetTenantFilter(request.TenantField, request.TenantCodes, inStatement)}",
                    new
                    {
                        request.NotOlderThan
                    }, commandTimeout: Timeout);
            }

            return await _connection.ExecuteReaderAsync(
                $@"SELECT * FROM {request.DataSource} WHERE 
                    ({GetFilterPredicate(request.Filter)})
                    AND {GetTenantFilter(request.TenantField, request.TenantCodes, inStatement)}", commandTimeout: Timeout);
        }

        private async Task<IDataReader> GetStoredProcedureDataReader(DataReaderRequest request)
        {
            var command = new SqlCommand(request.DataSource.SourceFullName, _connection);
            command.CommandType = CommandType.StoredProcedure;

            if (request.Filter?.Any() == true)
            {
                foreach (var filter in request.Filter)
                {
                    command.Parameters.Add(new SqlParameter(filter.FieldName, string.Join(",", filter.Values)));
                }
            }

            if (request.NotOlderThan != null)
                command.Parameters.AddWithValue("DateStart", request.NotOlderThan);

            if (request.TenantCodes != null && request.TenantCodes.Any())
                command.Parameters.AddWithValue("PropertyCode", string.Join(",", request.TenantCodes));

            return await command.ExecuteReaderAsync();
        }

        public async Task<long> CleanupTable(CleanupTableRequest request)
        {
            if (!string.IsNullOrEmpty(request.HistoryDateFromFieldName))
            {
                return await CleanupHistoryTable(request);
            }

            var inStatement = GetInStatement(request.InstanceFieldValues);

            int deleted;
            string query;
            if (request.NotOlderThan == null || request.FullReloading)
            {
                query = $@"DELETE FROM {request.DataSource} WHERE 
                        ({GetFilterPredicate(request.Filter)})
                        AND ({GetTenantFilter(request.InstanceFieldName, request.InstanceFieldValues, inStatement)})
                        AND ({GetDeleteProtectionDateFilter(request)})";
                Log.Warn(query);
                deleted = await _connection.ExecuteAsync(query, commandTimeout: Timeout);
            }
            else
            {
                query = $@"DELETE FROM {request.DataSource} WHERE
                         {request.ActualityFieldName} >= @NotOlderThan 
                         AND ({GetFilterPredicate(request.Filter)})
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

            Log.Warn($"Deleted {deleted} record(s) in target table");
            return deleted;
        }

        private async Task<long> CleanupHistoryTable(CleanupTableRequest request)
        {
            var inStatement = GetInStatement(request.InstanceFieldValues);

            if (request.FullReloading)
            {
                var query = $@"DELETE FROM {request.DataSource} WHERE 
                        {request.HistoryDateFromFieldName} = @CurrentPropertyDate
                        AND ({GetFilterPredicate(request.Filter)})
                        AND ({GetTenantFilter(request.InstanceFieldName, request.InstanceFieldValues, inStatement)})
                        AND ({GetDeleteProtectionDateFilter(request)})";
                Log.Warn(query);
                var deleted = await _connection.ExecuteAsync(query, new
                {
                    request.CurrentPropertyDate
                }, commandTimeout: Timeout);
                Log.Warn($"Deleted {deleted} record(s)");
            }

            return 0;
        }

        public async Task<long> InsertData(DataSource dataSource, IDataReader dataReader)
        {
            await CheckTablesCompatibility(dataSource, dataReader);
            var sw = new Stopwatch();
            sw.Start();
            long processed = 0;

            using var bulkCopy = new SqlBulkCopy(_connection)
            {
                BatchSize = 1000000,
                BulkCopyTimeout = Timeout,
                NotifyAfter = 10000,
                DestinationTableName = dataSource.ToString()
            };
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                var column = dataReader.GetName(i);
                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column, column));
            }

            bulkCopy.SqlRowsCopied += (sender, args) =>
            {
                Log.Info($"Records processed: {args.RowsCopied:#########}, table name {dataSource}");
                var handler = Progress;
                handler?.Invoke(this, new ProgressEventArgs(args.RowsCopied, "Copying data...", dataSource, sw.Elapsed));
            };

            await bulkCopy.WriteToServerAsync(dataReader);

            processed = bulkCopy.GetRowsCopied();

            return processed;
        }

        public async Task<string[]> GetTableFields(DataSource dataSource)
        {
            using var dataReader = await _connection.ExecuteReaderAsync($"SELECT TOP 0 * FROM {dataSource}", commandTimeout: Timeout);
            return Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToArray();
        }

        private async Task CheckTablesCompatibility(DataSource dataSource, IDataReader dataReader)
        {
            using var targetReader = await _connection.ExecuteReaderAsync($"SELECT TOP 0 * FROM {dataSource}", commandTimeout: Timeout);
            var sourceFields = Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToList();
            var targetFields = new HashSet<string>(Enumerable.Range(0, targetReader.FieldCount).Select(targetReader.GetName));

            var nonMatchingFields = sourceFields.Where(sf => !targetFields.Contains(sf)).ToList();
            if (nonMatchingFields.Any())
                throw new ApplicationException($"Source table contains fields that not present in target table: {string.Join(",", nonMatchingFields)}");
        }

        public async Task ExecuteRawQuery(string queryText)
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

        private string GetFilterPredicate(FilterConstraint[] filter)
        {
            if (filter == null)
                return "1=1";
            return string.Join(" AND ", filter.Select(f => $"{f.FieldName} IN ({string.Join(",", f.Values.Select(v => $"'{v}'"))})"));
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