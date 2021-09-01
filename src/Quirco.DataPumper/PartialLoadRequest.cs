using System;
using DataPumper.Core;

namespace Quirco.DataPumper
{
    public class PartialLoadRequest
    {
        public DateTime ActualDate { get; }
        
        public string[] TenantCodes { get; }
        
        public FilterConstraint Filter { get; }

        public PartialLoadRequest(DateTime actualDate, string[] tenantCodes, FilterConstraint filter)
        {
            ActualDate = actualDate;
            TenantCodes = tenantCodes;
            Filter = filter;
        }
    }
}