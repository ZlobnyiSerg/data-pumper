using Npgsql;
using System.Threading.Tasks;
using DataPumper.Core;
using SqlKata.Compilers;

namespace DataPumper.PostgreSql;

public class PostgreSqlDataPumperSource : DataPumperSource
{
    protected override Compiler Compiler => new PostgresCompiler();

    protected override string Name => "PostgreSQL Server";

    public override async Task Initialize(string connectionString)
    {
        Connection = new NpgsqlConnection(connectionString);
        await Connection.OpenAsync();
    }
}