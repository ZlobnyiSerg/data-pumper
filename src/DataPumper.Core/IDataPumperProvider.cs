using System.Threading.Tasks;

namespace DataPumper.Core
{
    public interface IDataPumperProvider
    {
        string GetName();
        
        Task Initialize(string connectionString);
    }
}