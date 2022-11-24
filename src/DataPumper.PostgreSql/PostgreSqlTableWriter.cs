using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace DataPumper.PostgreSql;

public class PostgreSqlTableWriter
{
    private readonly NpgsqlConnection _connection;
    private readonly long _notifyRowsWritten;

    public event EventHandler<long> Progress;

    public PostgreSqlTableWriter(NpgsqlConnection connection, long notifyRowsWritten)
    {
        _connection = connection;
        _notifyRowsWritten = notifyRowsWritten;
    }

    public long Write(string tableName, IDataReader sourceDataReader, Dictionary<string, NpgsqlDbType> typesMap, int? timeout = null)
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        if (timeout.HasValue)
            cts.CancelAfter(1000 * timeout.Value);

        var npgTypes = Enumerable.Range(0, sourceDataReader.FieldCount)
            .Select(x => typesMap[sourceDataReader.GetName(x)]).ToList();

        var targetFieldNames = Enumerable.Range(0, sourceDataReader.FieldCount)
            .Select(x => sourceDataReader.GetName(x).Enquote());

        return WriteInternal($"COPY {tableName} ({string.Join(", ", targetFieldNames)}) FROM STDIN (FORMAT BINARY)",
            sourceDataReader, npgTypes, token);
    }

    private long WriteInternal(string query, IDataReader sourceDataReader, IReadOnlyList<NpgsqlDbType> npgTypes, CancellationToken token)
    {
        using var writer = _connection.BeginBinaryImport(query);

        long rowCount = 0;

        while (sourceDataReader.Read())
        {
            writer.StartRow();
            for (var i = 0; i < sourceDataReader.FieldCount ; i++)
            {
                writer.Write(sourceDataReader.GetValue(i), npgTypes[i]);
            }

            rowCount++;

            if (rowCount % _notifyRowsWritten == 0)
                Progress?.Invoke(this, rowCount);

            token.ThrowIfCancellationRequested();
        }

        writer.Complete();

        return rowCount;
    }
}