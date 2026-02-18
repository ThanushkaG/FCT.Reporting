using System;
using System.Collections.Generic;
using System.Text;

namespace FCT.Reporting.Application.Contracts
{
    public class ReportRequestedEvent
    {
        public Guid JobId { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
    }
}
