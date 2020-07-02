using Microsoft.Practices.Unity;
using Quirco.DataPumper;

namespace DataPumper.Console
{
    public class Bootstrapper
    {
        public static void Initialize(IUnityContainer container)
        {
            container.RegisterType<IActualityDatesProvider, TestProvider>();
            container.RegisterType<DataPumperService>();
        }
    }
}