using System.Collections.Generic;
using Quirco.DataPumper.DataModels;

namespace Quirco.DataPumper
{
    public interface ILogsSender
    {
        void Send(ICollection<JobLog> jobLogs);
    }
}
