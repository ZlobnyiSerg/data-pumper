using Npgsql;
using System.Data.Entity;

namespace Quirco.DataPumper.DataModels;

internal class DataPumperDbConfiguration : DbConfiguration
{
    public DataPumperDbConfiguration()
    {
        var provider = ConfigurationManager.Configuration.Get<string>("Core:SourceProvider", "SqlServer");
        
        switch (provider)
        {
            case "PostgreSQL":
                UsePostgreSql();
                break;
            case "SqlServer":
                UseSqlServer();
                break;
        }
    }

    private void UseSqlServer()
    {
        SetDefaultConnectionFactory(new System.Data.Entity.Infrastructure.SqlConnectionFactory());
    }

    private void UsePostgreSql()
    {
        const string name = "Npgsql";

        SetProviderFactory(providerInvariantName: name,
            providerFactory: NpgsqlFactory.Instance);

        SetProviderServices(providerInvariantName: name,
            provider: NpgsqlServices.Instance);

        SetDefaultConnectionFactory(connectionFactory: new NpgsqlConnectionFactory());
    }
}