using DataPumper.Core;
using System;

namespace DataPumper.PostgreSql;

internal class DataSourceAdapter
{
    private readonly DataSource _dataSource;

    public DataSourceAdapter(DataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public string Schema => _dataSource.Schema?.Enquote();

    public string Name => _dataSource.Name.Enquote();

    public override string ToString()
    {
        return Schema != null ? $"{Schema}.{Name}" : Name;
    }
}