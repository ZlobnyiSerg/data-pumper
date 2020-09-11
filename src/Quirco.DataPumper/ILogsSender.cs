using Quirco.DataPumper.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quirco.DataPumper
{
    public interface ILogsSender
    {
        void Send(IEnumerable<JobLog> jobLogs);
    }
}
