using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASC.Calendar.Models
{
    public class OutsideEventModel
    {
        public string CalendarGuid { get; set; }
        public string EventGuid { get; set; }
        public string Ics { get; set; }
    }
}
