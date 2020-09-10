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
            IEnumerable<JobLog> logs;
            using (var ctx = new DataPumperContext())
            {
                logs = ctx.Logs.ToList();
            }
            return logs.Take(count);
        }
    }
}