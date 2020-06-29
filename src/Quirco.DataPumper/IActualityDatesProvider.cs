using System;
using System.Collections.Generic;
using System.Text;

namespace Quirco.DataPumper
{
    public interface IActualityDatesProvider
    {
        DateTime? GetJobActualDate(string jobName);
        void SetJobActualDate(string jobName, DateTime date);
    }
}
