using Microsoft.Practices.Unity;
using Quirco.DataPumper;

namespace DataPumper.Console
{
    public class Bootstrapper
    {
        public static void Initialize(IUnityContainer container)
        {
            container.RegisterType<DataPumperService>();
            container.RegisterType<Core.DataPumper>();
        }
    }
}