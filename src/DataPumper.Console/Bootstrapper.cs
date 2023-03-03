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
        private const string SqlTargetProvider = "SqlServer";
        private const string PostgresTargetProvider = "PostgreSQL";

        public static void Initialize(IUnityContainer container, WarehouseServiceConfiguration config)
        {
            if (config.TargetProvider != SqlTargetProvider && config.TargetProvider != PostgresTargetProvider)
                throw new ApplicationException(
                    $"Wrong TargetProvider: {config.TargetProvider}. Supported TargetProviders: {SqlTargetProvider}, {PostgresTargetProvider}.");

            container.RegisterType<DataPumperService>();
            container.RegisterType<Core.DataPumper>();

            // IDataPumperSource
            container.RegisterType<IEnumerable<IDataPumperSource>, IDataPumperSource[]>();
            container.RegisterType<IDataPumperSource, SqlDataPumperSourceTarget>();

            // IDataPumperTarget
            container.RegisterType<IEnumerable<IDataPumperTarget>, IDataPumperTarget[]>();
            container.RegisterType<IDataPumperTarget, PostgreSqlDataPumperTarget>(PostgresTargetProvider);
            container.RegisterType<IDataPumperTarget, SqlDataPumperSourceTarget>(SqlTargetProvider);
        }
    }
}