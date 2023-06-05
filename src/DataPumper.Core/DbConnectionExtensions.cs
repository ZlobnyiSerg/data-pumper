using System.Data.Common;

namespace DataPumper.PostgreSql;

public static class DbConnectionExtensions
{
    public static void AddParameterWithValue(this DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }
}