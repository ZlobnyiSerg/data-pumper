using System;
using DataPumper.Core;

namespace Quirco.DataPumper
{
    public class PartialLoadRequest
    {
        public DateTime ActualDate { get; }

        public FilterConstraint[] Filters { get; }

        public PartialLoadRequest(DateTime actualDate, params FilterConstraint[] filters)
        {
            ActualDate = actualDate;
            Filters = filters;
        }
    }
}