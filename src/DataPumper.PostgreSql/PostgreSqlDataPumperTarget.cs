using Common.Logging;
using Dapper;
using DataPumper.Core;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DataPumper.PostgreSql;

public class PostgreSqlDataPumperTarget : IDataPumperTarget, IDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(PostgreSqlDataPumperTarget));
    private NpgsqlConnection _connection;

    private const int Timeout = 60 * 60 * 3; // 3 hours

    public readonly DateTime ClosedIntervalDate = new DateTime(2200, 1, 1);

    public string GetName() => "PostgreSQL";

    public async Task Initialize(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();
    }

    public async Task<long> CleanupTable(CleanupTableRequest request)
    {
        var reqAdapter = new CleanupTableRequestAdapter(request);

        var whereClauses = new List<string>
        {
            reqAdapter.FilterPredicate,
            reqAdapter.TenantFilter,
            reqAdapter.DeleteProtectionDateFilter
        };

        if (reqAdapter.LastLoadDate.HasValue)
            whereClauses.Add($"{reqAdapter.ActualityFieldName} >= @NotOlderThan");

        var query =
            $"""
            DELETE FROM {reqAdapter.DataSource}
            {AndClause(whereClauses, "WHERE")}
            """;

        Log.Warn(query);

        var deleted = await _connection.ExecuteAsync(query,
            new { NotOlderThan = reqAdapter.LastLoadDate },
            commandTimeout: Timeout);
        return deleted;
    }

    public async Task<long> CleanupHistoryTable(CleanupTableRequest request)
    {
        var reqAdapter = new CleanupTableRequestAdapter(request);

        var whereClauses = new List<string>
        {
            reqAdapter.FilterPredicate,
            reqAdapter.TenantFilter,
            reqAdapter.DeleteProtectionDateFilter,
        };

        if (reqAdapter.LastLoadDate.HasValue)
            whereClauses.Add($"{reqAdapter.ActualityFieldName} >= @NotOlderThan");

        var filter = AndClause(whereClauses);
        if (!string.IsNullOrEmpty(filter)) filter = $"AND {filter}";

        var query = 
            $"""
            DELETE FROM {reqAdapter.DataSource} WHERE
            "HistoryDateFrom" = @CurrentPropertyDate
            {filter}
            """;

        Log.Warn(query);

        var deleted = await _connection.ExecuteAsync(query,
            new { reqAdapter.CurrentPropertyDate, NotOlderThan = reqAdapter.LastLoadDate },
            commandTimeout: Timeout);
        return deleted;
    }

    public async Task<int> CloseHistoricPeriods(CleanupTableRequest request)
    {
        var reqAdapter = new CleanupTableRequestAdapter(request);

        var filter = AndClause(reqAdapter.FilterPredicate, reqAdapter.TenantFilter);
        if (!string.IsNullOrEmpty(filter)) filter = $"AND {filter}";

        Log.Info($"Closing open intervals in {reqAdapter.DataSource}...");
        var query = 
            $"""
            UPDATE {reqAdapter.DataSource} SET "HistoryDateTo" = @ClosedDate WHERE
            ("HistoryDateFrom" = @CurrentPropertyDate OR "HistoryDateFrom" = {reqAdapter.ActualityFieldName}) 
            AND {reqAdapter.ActualityFieldName} > @LastLoadDate 
            AND {reqAdapter.ActualityFieldName} < @CurrentPropertyDate
            {filter}
            """;
        Log.Warn(query);
        Log.Warn($"@ClosedDate={ClosedIntervalDate}; @LastLoadDate={reqAdapter.LastLoadDate}; @CurrentPropertyDate={reqAdapter.CurrentPropertyDate}");
        var res = await _connection.ExecuteAsync(query, new
        {
            ClosedDate = ClosedIntervalDate,
            reqAdapter.CurrentPropertyDate,
            reqAdapter.LastLoadDate
        }, commandTimeout: Timeout);
        Log.Info($"Update affected {res} record(s)");

        if (reqAdapter.LastLoadDate != null && reqAdapter.CurrentPropertyDate != reqAdapter.LastLoadDate)
        {
            Log.Info($"Updated history dates on skipped days in {reqAdapter.DataSource}...");
            query = 
                $"""
                UPDATE {reqAdapter.DataSource} SET "HistoryDateTo" = @CurrentDatePrevDay 
                WHERE "HistoryDateTo" = @LastLoadDate
                {filter}
                """;
            Log.Warn(query);
            Log.Warn(
                $"@CurrentDatePrevDay={reqAdapter.CurrentPropertyDate.AddDays(-1)}; @LastLoadDate={reqAdapter.LastLoadDate}");
            var res2 = await _connection.ExecuteAsync(query, new
            {
                CurrentDatePrevDay = reqAdapter.CurrentPropertyDate.AddDays(-1),
                reqAdapter.LastLoadDate
            }, commandTimeout: Timeout);
            Log.Info($"Update closed {res2} outdated record(s)");
        }

        return res;
    }

    public async Task<long> InsertData(DataSource targetDataSource, IDataReader sourceDataReader)
    {
        var dsAdapter = new DataSourceAdapter(targetDataSource);
        var targetFields = await CheckCompatibilityAndReturnNpgTypes(dsAdapter, sourceDataReader);

        var sw = new Stopwatch();
        sw.Start();

        var tableWriter= new PostgreSqlTableWriter(_connection, 10000);
        tableWriter.Progress += (sender, rowCount) =>
        {
            Log.Info($"Records processed: {rowCount:#########}, table name {targetDataSource}");
            Progress?.Invoke(this, new ProgressEventArgs(rowCount, "Copying data...", targetDataSource, sw.Elapsed));
        };

        return tableWriter.Write(dsAdapter.ToString(), sourceDataReader, targetFields, Timeout);
    }

    public async Task ExecuteRawQuery(string queryText)
    {
        if (!string.IsNullOrEmpty(queryText))
        {
            var sw = new Stopwatch();
            sw.Start();
            Log.Warn($"Running query:\n{queryText}");
            await _connection.ExecuteAsync(queryText, commandTimeout: Timeout);
            Log.Info($"Query finished in {sw.Elapsed}");
        }
    }

    public event EventHandler<ProgressEventArgs> Progress;

    private async Task<Dictionary<string, NpgsqlDbType>> CheckCompatibilityAndReturnNpgTypes(DataSourceAdapter dataSource, IDataReader dataReader)
    {
        using var targetReader = await _connection.ExecuteReaderAsync($"SELECT * FROM {dataSource} FETCH FIRST 0 ROWS ONLY", commandTimeout: Timeout);
        var npgReader = (NpgsqlDataReader)targetReader;

        var sourceFields = Enumerable.Range(0, dataReader.FieldCount)
            .Select(dataReader.GetName)
            .ToList();

        var targetFields = npgReader.GetColumnSchema()
            .Where(x => x.NpgsqlDbType.HasValue)
            .ToDictionary(x => x.ColumnName, x => x.NpgsqlDbType.Value);

        var nonMatchingFields = sourceFields.Where(x => !targetFields.ContainsKey(x));
        if (nonMatchingFields.Any()) throw new ApplicationException(
            $"Source table contains fields that not present in target table: {string.Join(", ", nonMatchingFields)}");

        return targetFields;
    }

    private static string AndClause(params string[] clauses)
    {
        return AndClause(clauses.AsEnumerable());
    }

    private static string AndClause(IEnumerable<string> clauses, string prefix = null)
    {
        var clause = string.Join($"{Environment.NewLine}AND ", clauses.Where(x => !string.IsNullOrEmpty(x)));
        if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(clause)) return clause;

        return $"{prefix} {clause}";
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}