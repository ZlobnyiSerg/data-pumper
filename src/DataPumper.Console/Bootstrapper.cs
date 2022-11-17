﻿using DataPumper.Core;
using DataPumper.Sql;
using Microsoft.Practices.Unity;
using Quirco.DataPumper;
using System.Collections.Generic;
using DataPumper.PostgreSql;

namespace DataPumper.Console
{
    public class Bootstrapper
    {
        public static void Initialize(IUnityContainer container, WarehouseServiceConfiguration config)
        {
            container.RegisterType<DataPumperService>();
            container.RegisterType<Core.DataPumper>();

            // IDataPumperSource
            container.RegisterType<IEnumerable<IDataPumperSource>, IDataPumperSource[]>();
            container.RegisterType<IDataPumperSource, SqlDataPumperSourceTarget>();

            // IDataPumperTarget
            container.RegisterType<IEnumerable<IDataPumperTarget>, IDataPumperTarget[]>();

            switch (config.TargetProvider)
            {
                case "PostgreSQL":
                    container.RegisterType<IDataPumperTarget, PostgreSqlDataPumperTarget>();
                    break;
                case "SqlServer":
                    container.RegisterType<IDataPumperTarget, SqlDataPumperSourceTarget>();
                    break;
            }
        }
    }
}