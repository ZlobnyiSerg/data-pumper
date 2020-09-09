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
        private string _connectionString;
        public LogsExtractor(string connectionString)
        {
            _connectionString = connectionString;
        }
        public IEnumerable<JobLog> GetLogs(int count)
        {
            using (var ctx = new DataPumperContext(_connectionString))
            {
                return ctx.Logs.Take(count);
            }
        }
    }
}