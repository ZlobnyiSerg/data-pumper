using Quirco.DataPumper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPumper.Console
{
    public class TestActualityDatesProvider : IActualityDatesProvider
    {
        public DateTime? GetJobActualDate(string jobName)
        {
            return null;
        }

        public void SetJobActualDate(string jobName, DateTime date)
        {
            //throw new NotImplementedException();
        }
    }
}
