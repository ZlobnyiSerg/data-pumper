using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using Dapper;
using DataPumper.PostgreSql;
using SqlKata;
using SqlKata.Compilers;

namespace DataPumper.Core;

public abstract class DataPumperSource : IDataPumperSource
{
    protected static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperSource));
    protected DbConnection? Connection { get; set; }
    protected abstract Compiler Compiler { get; }
    protected abstract string Name { get; }
    protected const int Timeout = 60 * 60 * 3; // 3 hours

    public event EventHandler<ProgressEventArgs>? Progress;


    public abstract Task Initialize(string connectionString);

    public string GetName()
    {
        return Name;
    }

    public Task<DateTime?> GetCurrentDate(string query)
    {
        return Connection.ExecuteScalarAsync<DateTime?>(query, commandTimeout: Timeout);
    }

    public Task<IDataReader> GetDataReader(DataReaderRequest request)
    {
        var handler = Progress;
        handler?.Invoke(this,
            new ProgressEventArgs(0, $"Selecting data from source table '{request.DataSource}' ...", request.DataSource));

        return request.DataSource.IsStoredProcedure
            ? GetStoredProcedureDataReader(request)
            : GetQueryDataReader(request);
    }

    private Task<IDataReader> GetQueryDataReader(DataReaderRequest request)
    {
        var query = new Query(request.DataSource.ToStringUniversal())
            .When(request.NotOlderThan is not null,
                q => q.Where(request.ActualityDateFieldName, ">=", request.NotOlderThan))
            .When(!string.IsNullOrEmpty(request.TenantField) && request.TenantCodes?.Any() == true,
                q => q.WhereIn(request.TenantField, request.TenantCodes));

        foreach (var filter in request.Filter ?? Enumerable.Empty<FilterConstraint>())
        {
            query = query.WhereIn(filter.FieldName, filter.Values);
        }

        return ExecuteQuery(query);
    }

    private async Task<IDataReader> GetStoredProcedureDataReader(DataReaderRequest request)
    {
        if (Connection is null) throw new ApplicationException($"{this.GetType().Name} must be initialized.");

        var command = Connection.CreateCommand();
        command.CommandText = request.DataSource.SourceFullName;
        command.CommandType = CommandType.StoredProcedure;
        var parameters = (request.Filter ?? Enumerable.Empty<FilterConstraint>())
            .Select(filter =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = filter.FieldName;
                parameter.Value = string.Join(",", filter.Values);
                return parameter;
            });

        command.Parameters.AddRange(parameters.ToArray());

        if (request.NotOlderThan != null)
            command.AddParameterWithValue("DateStart", request.NotOlderThan);

        if (request.TenantCodes != null && request.TenantCodes.Any())
            command.AddParameterWithValue("PropertyCode", string.Join(",", request.TenantCodes));

        command.CommandTimeout = Timeout;

        return await command.ExecuteReaderAsync();
    }

    public async Task<string[]> GetTableFields(DataSource dataSource)
    {
        var query = new Query(dataSource.ToStringUniversal()).Take(1);
        using var dataReader = await ExecuteQuery(query);
        return Enumerable.Range(0, dataReader.FieldCount).Select(dataReader.GetName).ToArray();
    }

    private async Task<IDataReader> ExecuteQuery(Query query)
    {
        var sqlResult = Compiler.Compile(query);
        Log.Warn(sqlResult.ToString());

        var command = GetCommand(sqlResult);
        return await command.ExecuteReaderAsync();
    }

    private DbCommand GetCommand(SqlResult sqlResult)
    {
        if (Connection is null) throw new ApplicationException($"{this.GetType().Name} must be initialized.");

        var command = Connection.CreateCommand();
        command.CommandText = sqlResult.Sql;
        var parameters = sqlResult.NamedBindings.Select(b =>
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = b.Key;
            parameter.Value = b.Value;
            return parameter;
        });
        command.Parameters.AddRange(parameters.ToArray());
        command.CommandTimeout = Timeout;

        return command;
    }
}