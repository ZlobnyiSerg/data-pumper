using Quirco.DataPumper.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quirco.DataPumper
{
    public class LogsExtractor
    {
        public IEnumerable<JobLog> GetLogs(int count)
        {
            using (var ctx = new DataPumperContext())
            {
                return ctx.Logs.Take(count);
            }
        }
    }
}