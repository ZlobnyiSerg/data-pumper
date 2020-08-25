using DataPumper.Core;
using DataPumper.Sql;
using Microsoft.Practices.Unity;
using Quirco.DataPumper;
using System.Collections.Generic;

namespace DataPumper.Console
{
    public class Bootstrapper
    {
        public static void Initialize(IUnityContainer container)
        {
            container.RegisterType<DataPumperService>();
            container.RegisterType<Core.DataPumper>();

            // IDataPumperSource
            container.RegisterType<IEnumerable<IDataPumperSource>, IDataPumperSource[]>();
            container.RegisterType<IDataPumperSource, SqlDataPumperSourceTarget>(nameof(SqlDataPumperSourceTarget));

            // IDataPumperTarget
            container.RegisterType<IEnumerable<IDataPumperTarget>, IDataPumperTarget[]>();
            container.RegisterType<IDataPumperTarget, SqlDataPumperSourceTarget>(nameof(SqlDataPumperSourceTarget));
        }
    }
}