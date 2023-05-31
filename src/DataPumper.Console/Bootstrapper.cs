using DataPumper.Core;
using DataPumper.PostgreSql;
using DataPumper.Sql;
using Microsoft.Practices.Unity;
using Quirco.DataPumper;
using System;
using System.Collections.Generic;

namespace DataPumper.Console
{
    public class Bootstrapper
    {
        private const string SqlProvider = "SqlServer";
        private const string PostgresProvider = "PostgreSQL";

        public static void Initialize(IUnityContainer container, WarehouseServiceConfiguration config)
        {
            if (config.SourceProvider != SqlProvider && config.SourceProvider != PostgresProvider)
                throw new ApplicationException(
                    $"Wrong SourceProvider: {config.TargetProvider}. Supported SourceProviders: {SqlProvider}, {PostgresProvider}.");

            if (config.TargetProvider != SqlProvider && config.TargetProvider != PostgresProvider)
                throw new ApplicationException(
                    $"Wrong TargetProvider: {config.TargetProvider}. Supported TargetProviders: {SqlProvider}, {PostgresProvider}.");

            container.RegisterType<DataPumperService>();
            container.RegisterType<Core.DataPumper>();

            // IDataPumperSource
            container.RegisterType<IEnumerable<IDataPumperSource>, IDataPumperSource[]>();
            container.RegisterType<IDataPumperSource, PostgreSqlDataPumperSource>(PostgresProvider);
            container.RegisterType<IDataPumperSource, SqlDataPumperSourceTarget>(SqlProvider);

            // IDataPumperTarget
            container.RegisterType<IEnumerable<IDataPumperTarget>, IDataPumperTarget[]>();
            container.RegisterType<IDataPumperTarget, PostgreSqlDataPumperTarget>(PostgresProvider);
            container.RegisterType<IDataPumperTarget, SqlDataPumperSourceTarget>(SqlProvider);
        }
    }
}