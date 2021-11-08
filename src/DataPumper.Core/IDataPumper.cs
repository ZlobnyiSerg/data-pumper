using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumper
    {
        /// <summary>
        /// Осуществляет переливку данных из source в target
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<PumpResult> Pump(IDataPumperSource source, IDataPumperTarget target, PumpParameters parameters);
    }
}