using DataPumper.Core;
using DataPumper.Sql;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quirco.DataPumper
{
    public class Bootstrapper
    {
        public static void Initialize(IUnityContainer container)
        {
            // IDataPumperSource
            container.RegisterType<IEnumerable<IDataPumperSource>, IDataPumperSource[]>();
            container.RegisterType<IDataPumperSource, SqlDataPumperSourceTarget>(nameof(SqlDataPumperSourceTarget));

            // IDataPumperTarget
            container.RegisterType<IEnumerable<IDataPumperTarget>, IDataPumperTarget[]>();
            container.RegisterType<IDataPumperTarget, SqlDataPumperSourceTarget>(nameof(SqlDataPumperSourceTarget));            
        }
    }
}
